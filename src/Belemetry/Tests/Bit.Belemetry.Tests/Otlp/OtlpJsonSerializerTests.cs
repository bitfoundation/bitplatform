namespace Bit.Belemetry.Tests.Otlp;

/// <summary>The OTLP/JSON rules: lowerCamelCase names, hex ids, enums as numbers, 64-bit integers as strings.</summary>
[TestClass]
public class OtlpJsonSerializerTests
{
    [TestMethod]
    public void Traces_FollowTheJsonRules()
    {
        var span = Records.Span(BelemetrySpanKind.Server);
        span.TraceState = "a=b";
        span.Attributes.Set("i", 42).Set("d", 1.5).Set("b", true).Set("s", "x").Set("bytes", new byte[] { 1, 2 })
            .Set("la", new[] { 1L, 2L }).Set("nan", double.NaN);
        span.Events.Add(new BelemetrySpanEvent { Name = "e", TimeUnixNano = Records.Start + 1 });
        span.Links.Add(new BelemetrySpanLink { TraceId = Records.LinkTraceId, SpanId = Records.LinkSpanId });
        span.Status = BelemetryStatusCode.Error;
        span.StatusMessage = "failed";

        using var doc = JsonDocument.Parse(OtlpJsonSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [span])));
        var rs = doc.RootElement.GetProperty("resourceSpans")[0];
        Assert.AreEqual("checkout", FindAttr(rs.GetProperty("resource").GetProperty("attributes"), "service.name").GetProperty("stringValue").GetString());

        var ss = rs.GetProperty("scopeSpans")[0];
        Assert.AreEqual("System.Net.Http", ss.GetProperty("scope").GetProperty("name").GetString());
        Assert.AreEqual("10.0.0", ss.GetProperty("scope").GetProperty("version").GetString());

        var s = ss.GetProperty("spans")[0];
        Assert.AreEqual(Records.TraceId, s.GetProperty("traceId").GetString());
        Assert.AreEqual(Records.SpanId, s.GetProperty("spanId").GetString());
        Assert.AreEqual(Records.ParentSpanId, s.GetProperty("parentSpanId").GetString());
        Assert.AreEqual("a=b", s.GetProperty("traceState").GetString());
        Assert.AreEqual(JsonValueKind.Number, s.GetProperty("kind").ValueKind);
        Assert.AreEqual(2, s.GetProperty("kind").GetInt32());
        Assert.AreEqual(1, s.GetProperty("flags").GetInt32());
        Assert.AreEqual(JsonValueKind.String, s.GetProperty("startTimeUnixNano").ValueKind);
        Assert.AreEqual(Records.Start.ToString(), s.GetProperty("startTimeUnixNano").GetString());
        Assert.AreEqual(Records.End.ToString(), s.GetProperty("endTimeUnixNano").GetString());

        var attributes = s.GetProperty("attributes");
        Assert.AreEqual("42", FindAttr(attributes, "i").GetProperty("intValue").GetString());
        Assert.AreEqual(1.5, FindAttr(attributes, "d").GetProperty("doubleValue").GetDouble());
        Assert.IsTrue(FindAttr(attributes, "b").GetProperty("boolValue").GetBoolean());
        Assert.AreEqual("x", FindAttr(attributes, "s").GetProperty("stringValue").GetString());
        Assert.AreEqual("AQI=", FindAttr(attributes, "bytes").GetProperty("bytesValue").GetString());
        var array = FindAttr(attributes, "la").GetProperty("arrayValue").GetProperty("values");
        Assert.AreEqual("1", array[0].GetProperty("intValue").GetString());
        Assert.AreEqual("2", array[1].GetProperty("intValue").GetString());
        Assert.AreEqual("NaN", FindAttr(attributes, "nan").GetProperty("doubleValue").GetString());

        Assert.AreEqual((Records.Start + 1).ToString(), s.GetProperty("events")[0].GetProperty("timeUnixNano").GetString());
        Assert.AreEqual(Records.LinkTraceId, s.GetProperty("links")[0].GetProperty("traceId").GetString());
        Assert.AreEqual(2, s.GetProperty("status").GetProperty("code").GetInt32());
        Assert.AreEqual("failed", s.GetProperty("status").GetProperty("message").GetString());

        AssertCamelCase(doc.RootElement);
    }

    [TestMethod]
    public void Logs_FollowTheJsonRules()
    {
        var log = Records.Log("body", "my.event", BelemetrySeverity.Error);
        log.TraceId = Records.TraceId;
        log.SpanId = Records.SpanId;
        log.Flags = 1;

        using var doc = JsonDocument.Parse(OtlpJsonSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: [log])));
        var r = doc.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs")[0].GetProperty("logRecords")[0];

        Assert.AreEqual(Records.Start.ToString(), r.GetProperty("timeUnixNano").GetString());
        Assert.AreEqual((Records.Start + 5).ToString(), r.GetProperty("observedTimeUnixNano").GetString());
        Assert.AreEqual(17, r.GetProperty("severityNumber").GetInt32());
        Assert.AreEqual("ERROR", r.GetProperty("severityText").GetString());
        Assert.AreEqual("body", r.GetProperty("body").GetProperty("stringValue").GetString());
        Assert.AreEqual(Records.TraceId, r.GetProperty("traceId").GetString());
        Assert.AreEqual(Records.SpanId, r.GetProperty("spanId").GetString());
        Assert.AreEqual("my.event", r.GetProperty("eventName").GetString());
        Assert.AreEqual(1, r.GetProperty("flags").GetInt32());

        AssertCamelCase(doc.RootElement);
    }

    [TestMethod]
    [DataRow(BelemetryMetricTemporality.Cumulative)]
    [DataRow(BelemetryMetricTemporality.Delta)]
    public void Metrics_FollowTheJsonRules(BelemetryMetricTemporality temporality)
    {
        var delta = temporality == BelemetryMetricTemporality.Delta;
        using var doc = JsonDocument.Parse(OtlpJsonSerializer.SerializeMetrics(new BelemetryBatch(Records.Resource(), metrics: OtlpProtobufConformanceTests.MetricsSample()), temporality));
        var metrics = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("scopeMetrics")[0].GetProperty("metrics")
            .EnumerateArray().ToDictionary(m => m.GetProperty("name").GetString()!);

        var gauge = metrics["queue.length"].GetProperty("gauge").GetProperty("dataPoints")[0];
        Assert.AreEqual(7.5, gauge.GetProperty("asDouble").GetDouble());
        Assert.IsFalse(gauge.TryGetProperty("startTimeUnixNano", out _));

        var sum = metrics["requests"].GetProperty("sum");
        Assert.AreEqual((int)temporality, sum.GetProperty("aggregationTemporality").GetInt32());
        Assert.IsTrue(sum.GetProperty("isMonotonic").GetBoolean());
        var sp = sum.GetProperty("dataPoints")[0];
        Assert.AreEqual(JsonValueKind.String, sp.GetProperty("asInt").ValueKind);
        Assert.AreEqual(delta ? "4" : "10", sp.GetProperty("asInt").GetString());
        Assert.AreEqual(delta ? "2000" : "1000", sp.GetProperty("startTimeUnixNano").GetString());

        var hp = metrics["latency"].GetProperty("histogram").GetProperty("dataPoints")[0];
        Assert.AreEqual(delta ? "2" : "5", hp.GetProperty("count").GetString());
        Assert.AreEqual(delta ? 30.0 : 141.0, hp.GetProperty("sum").GetDouble());
        CollectionAssert.AreEqual(delta ? new[] { "0", "2", "0" } : new[] { "1", "3", "1" }, hp.GetProperty("bucketCounts").EnumerateArray().Select(e => e.GetString()).ToArray());
        CollectionAssert.AreEqual(new[] { 10.0, 100.0 }, hp.GetProperty("explicitBounds").EnumerateArray().Select(e => e.GetDouble()).ToArray());
        Assert.AreEqual(delta ? 10.0 : 1.0, hp.GetProperty("min").GetDouble());
        Assert.AreEqual(delta ? 20.0 : 100.0, hp.GetProperty("max").GetDouble());

        Assert.AreEqual(!delta, metrics.ContainsKey("idle"));

        AssertCamelCase(doc.RootElement);
    }

    private static JsonElement FindAttr(JsonElement attributes, string key)
    {
        foreach (var attribute in attributes.EnumerateArray())
        {
            if (attribute.GetProperty("key").GetString() == key) return attribute.GetProperty("value");
        }
        Assert.Fail($"Attribute '{key}' is missing.");
        return default;
    }

    /// <summary>Every property name is lowerCamelCase (attribute keys live in values, so they are not checked).</summary>
    private static void AssertCamelCase(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Assert.IsTrue(char.IsLower(property.Name[0]) && property.Name.All(char.IsLetterOrDigit), $"'{property.Name}' is not lowerCamelCase.");
                    AssertCamelCase(property.Value);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) AssertCamelCase(item);
                break;
        }
    }
}
