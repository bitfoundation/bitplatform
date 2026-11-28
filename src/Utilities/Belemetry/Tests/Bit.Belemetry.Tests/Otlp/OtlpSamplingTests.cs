using System.Globalization;
using System.Text.Json;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Bit.Belemetry.Tests.Otlp;

/// <summary>
/// The sampling ratio in OTLP requests: OpenTelemetry's probability-sampling threshold in a span's tracestate, with the
/// randomness <see cref="BelemetrySampler"/> decided on, and an attribute where a threshold cannot carry it.
/// </summary>
[TestClass]
public class OtlpSamplingTests
{
    private const long MaxAdjustedCount = 1L << 56;

    [TestMethod]
    public void Threshold_IsTheSpecificationsEncoding()
    {
        Assert.AreEqual("8", BelemetryTraceState.Threshold(0.5));
        Assert.AreEqual("c", BelemetryTraceState.Threshold(0.25), "The specification's example: th:c is 25%.");
        Assert.AreEqual("4", BelemetryTraceState.Threshold(0.75));
        Assert.AreEqual("e6666666666666", BelemetryTraceState.Threshold(0.1));
        Assert.AreEqual("ffffffffffffff", BelemetryTraceState.Threshold(1e-30), "Below 2^-56: the smallest probability there is.");

        foreach (var ratio in new[] { 0.5, 0.25, 0.1, 0.01, 0.001, 1e-6, 0.999 })
        {
            var probability = (double)(MaxAdjustedCount - ParseThreshold(BelemetryTraceState.Threshold(ratio))) / MaxAdjustedCount;
            Assert.AreEqual(ratio, probability, 1.0 / MaxAdjustedCount, $"The threshold of {ratio} decodes back to it, to the 2^-56 the encoding resolves.");
        }
    }

    [TestMethod]
    public void Randomness_MakesEverySpanTheSamplerKeeps_ConsistentWithItsThreshold()
    {
        var random = new Random(42);
        var ratios = new[] { 0.5, 0.25, 0.1, 0.01, 0.3333, 0.9 };
        Span<byte> bytes = stackalloc byte[16];
        var disagreements = 0;
        for (var i = 0; i < 20_000; i++)
        {
            random.NextBytes(bytes);
            var traceId = Convert.ToHexStringLower(bytes);
            var randomness = Convert.ToInt64(BelemetryTraceState.Randomness(traceId), 16);
            Assert.IsTrue(randomness is >= 0 and < MaxAdjustedCount);

            foreach (var ratio in ratios)
            {
                var kept = BelemetrySampler.ShouldSample(traceId, ratio);
                var consistent = randomness >= ParseThreshold(BelemetryTraceState.Threshold(ratio));
                if (kept) Assert.IsTrue(consistent, $"A kept trace {traceId} at {ratio} contradicts its threshold.");
                else if (consistent) disagreements++;
            }
        }

        // The other way round they differ only at the boundary, by rounding: the threshold says what the sampler does.
        Assert.IsLessThan(3, disagreements);
    }

    [TestMethod]
    public void Randomness_IsFourteenLowercaseHexDigits_OnlyForAValidTraceId()
    {
        var randomness = BelemetryTraceState.Randomness(Records.TraceId);
        Assert.IsNotNull(randomness);
        Assert.HasCount(14, randomness);
        Assert.AreEqual(randomness.ToLowerInvariant(), randomness);
        Assert.AreEqual("ffffffffffffff", BelemetryTraceState.Randomness(new string('0', 32)));
        Assert.AreEqual("00000000000000", BelemetryTraceState.Randomness("80000000000000000000000000000000"), "long.MinValue");
        Assert.IsNull(BelemetryTraceState.Randomness("abc"));
        Assert.IsNull(BelemetryTraceState.Randomness(new string('z', 32)));
    }

    [TestMethod]
    public void WithOtelThreshold_PutsTheOtEntryFirst_KeepingEverythingElse()
    {
        var rv = BelemetryTraceState.Randomness(Records.TraceId);

        Assert.AreEqual($"ot=rv:{rv};th:c", BelemetryTraceState.WithOtelThreshold(null, Records.TraceId, 0.25));
        Assert.AreEqual($"ot=rv:{rv};th:c,belemetry=sr:0.25,vendor=1",
                        BelemetryTraceState.WithOtelThreshold("belemetry=sr:0.25,vendor=1", Records.TraceId, 0.25));
        Assert.AreEqual($"ot=rv:{rv};th:8;foo:bar,vendor=1",
                        BelemetryTraceState.WithOtelThreshold("vendor=1, ot=th:c;foo:bar", Records.TraceId, 0.5),
                        "Its own threshold is replaced, OpenTelemetry's other values are kept.");
        Assert.AreEqual($"ot=rv:{rv};th:c",
                        BelemetryTraceState.WithOtelThreshold($"ot=rv:{rv}", Records.TraceId, 0.25), "The same randomness is kept once.");
    }

    [TestMethod]
    public void WithOtelThreshold_WritesNoThresholdItDidNotDecide()
    {
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold("ot=rv:00000000000000", Records.TraceId, 0.25), "Another randomness.");
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold(null, Records.TraceId, 1));
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold(null, Records.TraceId, 0));
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold(null, Records.TraceId, double.NaN));
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold(null, "not a trace id", 0.25));
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold("ot=x:" + new string('a', 250), Records.TraceId, 0.25), "Past the entry's 256 characters.");
        // About 0.99 of the way: the sampler keeps it at 0.5 only by someone else's say (a caller's entry, a random processor).
        const string unkept = "7f000000000000000000000000000001";
        Assert.IsFalse(BelemetrySampler.ShouldSample(unkept, 0.5));
        Assert.IsNull(BelemetryTraceState.WithOtelThreshold("belemetry=sr:0.5", unkept, 0.5), "Its rv would contradict the th.");
    }

    [TestMethod]
    public void WithOtelThreshold_KeepsTheW3CLimitOf512Characters_DroppingMembersFromTheEnd()
    {
        var state = string.Join(",", Enumerable.Range(0, 10).Select(i => $"k{i}={new string('v', 45)}"));
        Assert.IsLessThan(512, state.Length);

        var result = BelemetryTraceState.WithOtelThreshold(state, Records.TraceId, 0.25)!;
        Assert.IsLessThanOrEqualTo(512, result.Length);
        Assert.StartsWith("ot=", result);
        var members = result.Split(',');
        Assert.AreEqual("k0=" + new string('v', 45), members[1]);
        Assert.IsLessThan(11, members.Length, "The last member made room for the threshold.");
    }

    [TestMethod]
    public void WithOtelThreshold_KeepsTheW3CLimitOf32Members()
    {
        var state = string.Join(",", Enumerable.Range(0, 32).Select(i => $"k{i}=v"));
        var members = BelemetryTraceState.WithOtelThreshold(state, Records.TraceId, 0.5)!.Split(',');
        Assert.HasCount(32, members);
        Assert.StartsWith("ot=", members[0]);
        Assert.AreEqual("k30=v", members[^1]);
    }

    [TestMethod]
    public void Protobuf_ASampledSpan_CarriesTheThreshold_AndALogRecordTheAttribute()
    {
        var span = Records.Span();
        span.TraceState = "belemetry=sr:0.25";
        span.SamplingRatio = 0.25;
        var log = Records.Log();
        log.SamplingRatio = 0.25;

        var s = ExportTraceServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [span])))
            .ResourceSpans[0].ScopeSpans[0].Spans[0];
        Assert.AreEqual($"ot=rv:{BelemetryTraceState.Randomness(Records.TraceId)};th:c,belemetry=sr:0.25", s.TraceState);
        Assert.IsFalse(s.Attributes.Any(a => a.Key == BelemetryOtlpSampling.RatioAttribute));
        Assert.AreEqual("belemetry=sr:0.25", span.TraceState, "The record is every exporter's: it is left alone.");

        var l = ExportLogsServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: [log])))
            .ResourceLogs[0].ScopeLogs[0].LogRecords[0];
        Assert.AreEqual(0.25, l.Attributes.Single(a => a.Key == BelemetryOtlpSampling.RatioAttribute).Value.DoubleValue);
        Assert.IsFalse(log.Attributes.Contains(BelemetryOtlpSampling.RatioAttribute));
    }

    [TestMethod]
    public void Protobuf_AtRatio1_NothingChanges()
    {
        var span = Records.Span();
        span.TraceState = "vendor=1";
        var log = Records.Log();

        var s = ExportTraceServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [span])))
            .ResourceSpans[0].ScopeSpans[0].Spans[0];
        Assert.AreEqual("vendor=1", s.TraceState);
        Assert.IsFalse(s.Attributes.Any(a => a.Key == BelemetryOtlpSampling.RatioAttribute));

        var l = ExportLogsServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: [log])))
            .ResourceLogs[0].ScopeLogs[0].LogRecords[0];
        Assert.IsFalse(l.Attributes.Any(a => a.Key == BelemetryOtlpSampling.RatioAttribute));
    }

    [TestMethod]
    public void Protobuf_ASpanKeptByItsSession_OrUnderAnotherRandomness_CarriesTheAttribute()
    {
        var browser = new BelemetryResource(new BelemetryAttributes().Set(BelemetrySemanticConventions.TelemetrySdkLanguage, "webjs"));
        var relayed = Records.Span();
        relayed.SamplingRatio = 0.5;

        var s = ExportTraceServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(browser, [relayed])))
            .ResourceSpans[0].ScopeSpans[0].Spans[0];
        Assert.AreEqual("", s.TraceState, "The session decided, not the trace id: no threshold.");
        Assert.AreEqual(0.5, s.Attributes.Single(a => a.Key == BelemetryOtlpSampling.RatioAttribute).Value.DoubleValue);

        var foreign = Records.Span();
        foreign.TraceState = "ot=rv:00000000000001;th:0";
        foreign.SamplingRatio = 0.5;
        s = ExportTraceServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [foreign])))
            .ResourceSpans[0].ScopeSpans[0].Spans[0];
        Assert.AreEqual("ot=rv:00000000000001;th:0", s.TraceState);
        Assert.AreEqual(0.5, s.Attributes.Single(a => a.Key == BelemetryOtlpSampling.RatioAttribute).Value.DoubleValue);
    }

    [TestMethod]
    public void Json_CarriesTheSame()
    {
        var span = Records.Span();
        span.SamplingRatio = 0.25;
        using (var doc = JsonDocument.Parse(BelemetryOtlpJsonSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [span]))))
        {
            var s = doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];
            Assert.AreEqual($"ot=rv:{BelemetryTraceState.Randomness(Records.TraceId)};th:c", s.GetProperty("traceState").GetString());
        }

        var log = Records.Log();
        log.SamplingRatio = 0.1;
        log.Attributes.Set("a", 1L);
        using (var doc = JsonDocument.Parse(BelemetryOtlpJsonSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: [log]))))
        {
            var attributes = doc.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs")[0].GetProperty("logRecords")[0].GetProperty("attributes");
            Assert.AreEqual(2, attributes.GetArrayLength());
            Assert.AreEqual(BelemetryOtlpSampling.RatioAttribute, attributes[1].GetProperty("key").GetString());
            Assert.AreEqual(0.1, attributes[1].GetProperty("value").GetProperty("doubleValue").GetDouble());
        }
    }

    [TestMethod]
    public void ARatioAProcessorSetAsTheAttribute_IsNotWrittenTwice()
    {
        var log = Records.Log();
        log.SamplingRatio = 0.5;
        log.Attributes.Set(BelemetryOtlpSampling.RatioAttribute, 0.2);

        var l = ExportLogsServiceRequest.Parser.ParseFrom(BelemetryOtlpProtobufSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: [log])))
            .ResourceLogs[0].ScopeLogs[0].LogRecords[0];
        Assert.AreEqual(0.2, l.Attributes.Single(a => a.Key == BelemetryOtlpSampling.RatioAttribute).Value.DoubleValue);
    }

    [TestMethod]
    [DataRow(BelemetryOtlpProtocol.HttpProtobuf)]
    [DataRow(BelemetryOtlpProtocol.HttpJson)]
    public void ABlockedRatioAttribute_IsNotWritten_ButTheThresholdStill(BelemetryOtlpProtocol protocol)
    {
        var exporter = new BelemetryOtlpExporter(new BelemetryOtlpExporterOptions { Endpoint = new Uri("http://collector.test:4318"), Protocol = protocol },
                                                 BelemetryMetricTemporality.Cumulative, "otlp", ratioAttribute: false);
        var browser = new BelemetryResource(new BelemetryAttributes().Set(BelemetrySemanticConventions.TelemetrySdkLanguage, "webjs"));
        var relayed = Records.Span();
        relayed.SamplingRatio = 0.5;
        var sampled = Records.Span();
        sampled.SamplingRatio = 0.25;
        var log = Records.Log();
        log.SamplingRatio = 0.25;

        var requests = exporter.CreateRequests(new BelemetryBatch(browser, [relayed], [log]))
            .Concat(exporter.CreateRequests(new BelemetryBatch(Records.Resource(), [sampled])))
            .ToList();
        Assert.AreEqual(3, requests.Count);

        foreach (var request in requests)
        {
            CollectionAssert.DoesNotContain(Attributes(request, protocol), BelemetryOtlpSampling.RatioAttribute, request.Url);
        }

        var traceState = protocol == BelemetryOtlpProtocol.HttpProtobuf
            ? ExportTraceServiceRequest.Parser.ParseFrom(requests[2].Body).ResourceSpans[0].ScopeSpans[0].Spans[0].TraceState
            : JsonDocument.Parse(requests[2].Body).RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0].GetProperty("traceState").GetString();
        Assert.AreEqual($"ot=rv:{BelemetryTraceState.Randomness(Records.TraceId)};th:c", traceState, "The tracestate is no attribute: blocking the key leaves it.");
    }

    private static List<string> Attributes(BelemetryHttpRequest request, BelemetryOtlpProtocol protocol)
    {
        var logs = request.Url.EndsWith("v1/logs", StringComparison.Ordinal);
        if (protocol == BelemetryOtlpProtocol.HttpProtobuf)
        {
            return logs
                ? [.. ExportLogsServiceRequest.Parser.ParseFrom(request.Body).ResourceLogs[0].ScopeLogs[0].LogRecords[0].Attributes.Select(a => a.Key)]
                : [.. ExportTraceServiceRequest.Parser.ParseFrom(request.Body).ResourceSpans[0].ScopeSpans[0].Spans[0].Attributes.Select(a => a.Key)];
        }

        using var doc = JsonDocument.Parse(request.Body);
        var record = logs
            ? doc.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs")[0].GetProperty("logRecords")[0]
            : doc.RootElement.GetProperty("resourceSpans")[0].GetProperty("scopeSpans")[0].GetProperty("spans")[0];
        return record.TryGetProperty("attributes", out var attributes) ? [.. attributes.EnumerateArray().Select(a => a.GetProperty("key").GetString()!)] : [];
    }

    private static long ParseThreshold(string th) => long.Parse(th.PadRight(14, '0'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
