using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using PbSpan = OpenTelemetry.Proto.Trace.V1.Span;
using PbStatus = OpenTelemetry.Proto.Trace.V1.Status;

namespace Bit.Belemetry.Tests.Otlp;

/// <summary>
/// Decodes the bytes <see cref="OtlpProtobufSerializer"/> writes with the types generated from the official
/// opentelemetry-proto v1.11.1 files, and checks every field.
/// </summary>
[TestClass]
public class OtlpProtobufConformanceTests
{
    [TestMethod]
    public void Traces_DecodeWithEveryField()
    {
        var span = Records.Span(BelemetrySpanKind.Client);
        span.TraceState = "vendor=value";
        span.Flags = 1;
        span.Attributes
            .Set("s", "text")
            .Set("b", true)
            .Set("bf", false)
            .Set("i", 42)
            .Set("neg", -7L)
            .Set("d", 1.5)
            .Set("bytes", new byte[] { 1, 2, 3 })
            .Set("sa", new[] { "a", "b" })
            .Set("la", new[] { 1L, -2L })
            .Set("da", new[] { 0.5, 2.25 })
            .Set("ba", new[] { true, false });
        span.Events.Add(new BelemetrySpanEvent { Name = "retry", TimeUnixNano = Records.Start + 100, Attributes = new BelemetryAttributes().Set("attempt", 2) });
        span.Links.Add(new BelemetrySpanLink { TraceId = Records.LinkTraceId, SpanId = Records.LinkSpanId, TraceState = "k=v", Attributes = new BelemetryAttributes().Set("link.kind", "follows") });
        span.Status = BelemetryStatusCode.Error;
        span.StatusMessage = "boom";

        var second = Records.Span(BelemetrySpanKind.Server, "other scope");
        second.ParentSpanId = null;
        second.Scope = new BelemetryScope("MyApp", null);

        var bytes = OtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), spans: [span, second]));
        var request = ExportTraceServiceRequest.Parser.ParseFrom(bytes);

        Assert.HasCount(1, request.ResourceSpans);
        var rs = request.ResourceSpans[0];
        Assert.AreEqual("checkout", Attr(rs.Resource.Attributes, "service.name").StringValue);
        Assert.AreEqual("shop", Attr(rs.Resource.Attributes, "service.namespace").StringValue);
        Assert.AreEqual(0u, rs.Resource.DroppedAttributesCount);

        Assert.HasCount(2, rs.ScopeSpans);
        var scope = rs.ScopeSpans[0];
        Assert.AreEqual("System.Net.Http", scope.Scope.Name);
        Assert.AreEqual("10.0.0", scope.Scope.Version);
        Assert.HasCount(1, scope.Spans);

        var s = scope.Spans[0];
        CollectionAssert.AreEqual(Records.Hex(Records.TraceId), s.TraceId.ToByteArray());
        CollectionAssert.AreEqual(Records.Hex(Records.SpanId), s.SpanId.ToByteArray());
        CollectionAssert.AreEqual(Records.Hex(Records.ParentSpanId), s.ParentSpanId.ToByteArray());
        Assert.AreEqual("vendor=value", s.TraceState);
        Assert.AreEqual("GET /api/orders", s.Name);
        Assert.AreEqual(PbSpan.Types.SpanKind.Client, s.Kind);
        Assert.AreEqual((ulong)Records.Start, s.StartTimeUnixNano);
        Assert.AreEqual((ulong)Records.End, s.EndTimeUnixNano);
        Assert.AreEqual(1u, s.Flags);
        Assert.AreEqual(0u, s.DroppedAttributesCount);

        Assert.AreEqual("text", Attr(s.Attributes, "s").StringValue);
        Assert.IsTrue(Attr(s.Attributes, "b").BoolValue);
        Assert.AreEqual(AnyValue.ValueOneofCase.BoolValue, Attr(s.Attributes, "bf").ValueCase);
        Assert.IsFalse(Attr(s.Attributes, "bf").BoolValue);
        Assert.AreEqual(AnyValue.ValueOneofCase.IntValue, Attr(s.Attributes, "i").ValueCase);
        Assert.AreEqual(42L, Attr(s.Attributes, "i").IntValue);
        Assert.AreEqual(-7L, Attr(s.Attributes, "neg").IntValue);
        Assert.AreEqual(1.5, Attr(s.Attributes, "d").DoubleValue);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, Attr(s.Attributes, "bytes").BytesValue.ToByteArray());
        CollectionAssert.AreEqual(new[] { "a", "b" }, Attr(s.Attributes, "sa").ArrayValue.Values.Select(v => v.StringValue).ToArray());
        CollectionAssert.AreEqual(new[] { 1L, -2L }, Attr(s.Attributes, "la").ArrayValue.Values.Select(v => v.IntValue).ToArray());
        CollectionAssert.AreEqual(new[] { 0.5, 2.25 }, Attr(s.Attributes, "da").ArrayValue.Values.Select(v => v.DoubleValue).ToArray());
        CollectionAssert.AreEqual(new[] { true, false }, Attr(s.Attributes, "ba").ArrayValue.Values.Select(v => v.BoolValue).ToArray());
        Assert.IsTrue(Attr(s.Attributes, "ba").ArrayValue.Values.All(v => v.ValueCase == AnyValue.ValueOneofCase.BoolValue));

        Assert.HasCount(1, s.Events);
        Assert.AreEqual("retry", s.Events[0].Name);
        Assert.AreEqual((ulong)(Records.Start + 100), s.Events[0].TimeUnixNano);
        Assert.AreEqual(2L, Attr(s.Events[0].Attributes, "attempt").IntValue);

        Assert.HasCount(1, s.Links);
        CollectionAssert.AreEqual(Records.Hex(Records.LinkTraceId), s.Links[0].TraceId.ToByteArray());
        CollectionAssert.AreEqual(Records.Hex(Records.LinkSpanId), s.Links[0].SpanId.ToByteArray());
        Assert.AreEqual("k=v", s.Links[0].TraceState);
        Assert.AreEqual("follows", Attr(s.Links[0].Attributes, "link.kind").StringValue);

        Assert.AreEqual(PbStatus.Types.StatusCode.Error, s.Status.Code);
        Assert.AreEqual("boom", s.Status.Message);

        var other = rs.ScopeSpans[1];
        Assert.AreEqual("MyApp", other.Scope.Name);
        Assert.AreEqual(string.Empty, other.Scope.Version);
        Assert.AreEqual(PbSpan.Types.SpanKind.Server, other.Spans[0].Kind);
        Assert.IsTrue(other.Spans[0].ParentSpanId.IsEmpty);
        Assert.IsNull(other.Spans[0].Status);

        // Every field landed in a known field: re-encoding the parsed message reproduces the same bytes.
        CollectionAssert.AreEqual(bytes, request.ToByteArray());
    }

    [TestMethod]
    public void Traces_StatusOkHasNoMessage_AndAllKindsMap()
    {
        var spans = new List<BelemetrySpan>();
        foreach (var kind in new[] { BelemetrySpanKind.Internal, BelemetrySpanKind.Server, BelemetrySpanKind.Client, BelemetrySpanKind.Producer, BelemetrySpanKind.Consumer })
        {
            var span = Records.Span(kind);
            span.Status = BelemetryStatusCode.Ok;
            span.StatusMessage = "ignored for ok";
            spans.Add(span);
        }

        var request = ExportTraceServiceRequest.Parser.ParseFrom(OtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), spans)));
        var decoded = request.ResourceSpans[0].ScopeSpans[0].Spans;

        CollectionAssert.AreEqual(
            new[] { PbSpan.Types.SpanKind.Internal, PbSpan.Types.SpanKind.Server, PbSpan.Types.SpanKind.Client, PbSpan.Types.SpanKind.Producer, PbSpan.Types.SpanKind.Consumer },
            decoded.Select(s => s.Kind).ToArray());
        Assert.IsTrue(decoded.All(s => s.Status.Code == PbStatus.Types.StatusCode.Ok && s.Status.Message == string.Empty));
    }

    [TestMethod]
    public void Traces_DroppedAttributesCountIsReported()
    {
        var span = Records.Span();
        var limited = new BelemetryAttributes(2).Set("a", 1).Set("b", 2).Set("c", 3).Set("d", 4);
        span = new BelemetrySpan { TraceId = span.TraceId, SpanId = span.SpanId, Name = span.Name, Attributes = limited };

        var resource = new BelemetryResource(new BelemetryAttributes(1).Set("service.name", "x").Set("extra", "y"));
        var request = ExportTraceServiceRequest.Parser.ParseFrom(OtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(resource, [span])));

        Assert.AreEqual(1u, request.ResourceSpans[0].Resource.DroppedAttributesCount);
        var s = request.ResourceSpans[0].ScopeSpans[0].Spans[0];
        Assert.HasCount(2, s.Attributes);
        Assert.AreEqual(2u, s.DroppedAttributesCount);
    }

    [TestMethod]
    public void Traces_LongValuesNeedMultiByteLengthPrefixes()
    {
        var medium = new string('m', 200);     // > 127 bytes: a 2-byte length varint
        var large = new string('L', 20_000);   // > 16 KB: a 3-byte length varint, nested in 5 messages
        var span = Records.Span();
        span.Attributes.Set("medium", medium).Set("large", large).Set("unicode", "héllo ✓ 😀");

        var bytes = OtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [span]));
        var s = ExportTraceServiceRequest.Parser.ParseFrom(bytes).ResourceSpans[0].ScopeSpans[0].Spans[0];

        Assert.AreEqual(medium, Attr(s.Attributes, "medium").StringValue);
        Assert.AreEqual(large, Attr(s.Attributes, "large").StringValue);
        Assert.AreEqual("héllo ✓ 😀", Attr(s.Attributes, "unicode").StringValue);
        Assert.AreEqual(Records.TraceId, Convert.ToHexString(s.TraceId.ToByteArray()).ToLowerInvariant());
    }

    [TestMethod]
    public void Traces_MalformedIdsAreOmitted()
    {
        var span = Records.Span();
        span.TraceId = "not-hex";
        span.ParentSpanId = "zz";

        var s = ExportTraceServiceRequest.Parser.ParseFrom(OtlpProtobufSerializer.SerializeTraces(new BelemetryBatch(Records.Resource(), [span])))
            .ResourceSpans[0].ScopeSpans[0].Spans[0];

        Assert.IsTrue(s.TraceId.IsEmpty);
        Assert.IsTrue(s.ParentSpanId.IsEmpty);
        CollectionAssert.AreEqual(Records.Hex(Records.SpanId), s.SpanId.ToByteArray());
    }

    [TestMethod]
    public void Logs_DecodeWithEveryField()
    {
        var log = Records.Log("Order 42 shipped", eventName: "order.shipped", severity: BelemetrySeverity.Warn);
        log.SeverityText = "Warning";
        log.TraceId = Records.TraceId;
        log.SpanId = Records.SpanId;
        log.Flags = 1;
        log.Attributes.Set("order.id", 42).Set("ok", true).Set("tags", new[] { "a", "b" });

        var minimal = new BelemetryLogRecord { TimeUnixNano = Records.Start, Severity = BelemetrySeverity.Unspecified, Scope = log.Scope };

        var bytes = OtlpProtobufSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: [log, minimal]));
        var request = ExportLogsServiceRequest.Parser.ParseFrom(bytes);

        var scopeLogs = request.ResourceLogs[0].ScopeLogs[0];
        Assert.AreEqual("MyApp.Orders", scopeLogs.Scope.Name);
        Assert.AreEqual("checkout", Attr(request.ResourceLogs[0].Resource.Attributes, "service.name").StringValue);

        var r = scopeLogs.LogRecords[0];
        Assert.AreEqual((ulong)Records.Start, r.TimeUnixNano);
        Assert.AreEqual((ulong)(Records.Start + 5), r.ObservedTimeUnixNano);
        Assert.AreEqual(SeverityNumber.Warn, r.SeverityNumber);
        Assert.AreEqual("Warning", r.SeverityText);
        Assert.AreEqual(AnyValue.ValueOneofCase.StringValue, r.Body.ValueCase);
        Assert.AreEqual("Order 42 shipped", r.Body.StringValue);
        Assert.AreEqual("order.shipped", r.EventName);
        CollectionAssert.AreEqual(Records.Hex(Records.TraceId), r.TraceId.ToByteArray());
        CollectionAssert.AreEqual(Records.Hex(Records.SpanId), r.SpanId.ToByteArray());
        Assert.AreEqual(1u, r.Flags);
        Assert.AreEqual(42L, Attr(r.Attributes, "order.id").IntValue);
        Assert.IsTrue(Attr(r.Attributes, "ok").BoolValue);
        CollectionAssert.AreEqual(new[] { "a", "b" }, Attr(r.Attributes, "tags").ArrayValue.Values.Select(v => v.StringValue).ToArray());

        var m = scopeLogs.LogRecords[1];
        Assert.AreEqual(SeverityNumber.Unspecified, m.SeverityNumber);
        Assert.IsNull(m.Body);
        Assert.AreEqual(string.Empty, m.EventName);
        Assert.IsTrue(m.TraceId.IsEmpty);
        Assert.IsTrue(m.SpanId.IsEmpty);
        Assert.AreEqual(0UL, m.ObservedTimeUnixNano);

        CollectionAssert.AreEqual(bytes, request.ToByteArray());
    }

    [TestMethod]
    public void Logs_AllSeveritiesMap()
    {
        var severities = new[] { BelemetrySeverity.Trace, BelemetrySeverity.Debug, BelemetrySeverity.Info, BelemetrySeverity.Warn, BelemetrySeverity.Error, BelemetrySeverity.Fatal };
        var logs = severities.Select(s => Records.Log(severity: s)).ToList();

        var decoded = ExportLogsServiceRequest.Parser.ParseFrom(OtlpProtobufSerializer.SerializeLogs(new BelemetryBatch(Records.Resource(), logs: logs)))
            .ResourceLogs[0].ScopeLogs[0].LogRecords;

        CollectionAssert.AreEqual(
            new[] { SeverityNumber.Trace, SeverityNumber.Debug, SeverityNumber.Info, SeverityNumber.Warn, SeverityNumber.Error, SeverityNumber.Fatal },
            decoded.Select(r => r.SeverityNumber).ToArray());
    }

    [TestMethod]
    [DataRow(BelemetryMetricTemporality.Cumulative)]
    [DataRow(BelemetryMetricTemporality.Delta)]
    public void Metrics_DecodeGaugeSumHistogram(BelemetryMetricTemporality temporality)
    {
        var delta = temporality == BelemetryMetricTemporality.Delta;
        var batch = new BelemetryBatch(Records.Resource(), metrics: MetricsSample());

        var bytes = OtlpProtobufSerializer.SerializeMetrics(batch, temporality);
        var request = ExportMetricsServiceRequest.Parser.ParseFrom(bytes);

        var scopeMetrics = request.ResourceMetrics[0].ScopeMetrics[0];
        Assert.AreEqual("MyApp.Meter", scopeMetrics.Scope.Name);
        Assert.AreEqual("1.0", scopeMetrics.Scope.Version);
        var metrics = scopeMetrics.Metrics.ToDictionary(m => m.Name);

        // Gauge (double): always the last value; no start time.
        var gauge = metrics["queue.length"];
        Assert.AreEqual(Metric.DataOneofCase.Gauge, gauge.DataCase);
        Assert.AreEqual("Items waiting", gauge.Description);
        Assert.AreEqual("{item}", gauge.Unit);
        var gp = gauge.Gauge.DataPoints.Single();
        Assert.AreEqual(NumberDataPoint.ValueOneofCase.AsDouble, gp.ValueCase);
        Assert.AreEqual(7.5, gp.AsDouble);
        Assert.AreEqual(0UL, gp.StartTimeUnixNano);
        Assert.AreEqual(3000UL, gp.TimeUnixNano);
        Assert.AreEqual("eu", Attr(gp.Attributes, "region").StringValue);

        // Sum (integer, monotonic): cumulative or delta value and start time.
        var sum = metrics["requests"];
        Assert.AreEqual(Metric.DataOneofCase.Sum, sum.DataCase);
        Assert.IsTrue(sum.Sum.IsMonotonic);
        Assert.AreEqual(delta ? AggregationTemporality.Delta : AggregationTemporality.Cumulative, sum.Sum.AggregationTemporality);
        var sp = sum.Sum.DataPoints.Single();
        Assert.AreEqual(NumberDataPoint.ValueOneofCase.AsInt, sp.ValueCase);
        Assert.AreEqual(delta ? 4L : 10L, sp.AsInt);
        Assert.AreEqual(delta ? 2000UL : 1000UL, sp.StartTimeUnixNano);
        Assert.AreEqual(3000UL, sp.TimeUnixNano);

        // Sum (double, up-down).
        var updown = metrics["balance"];
        Assert.IsFalse(updown.Sum.IsMonotonic);
        var up = updown.Sum.DataPoints.Single();
        Assert.AreEqual(NumberDataPoint.ValueOneofCase.AsDouble, up.ValueCase);
        Assert.AreEqual(delta ? -1.25 : 3.5, up.AsDouble);

        // Histogram.
        var histogram = metrics["latency"];
        Assert.AreEqual(Metric.DataOneofCase.Histogram, histogram.DataCase);
        Assert.AreEqual(delta ? AggregationTemporality.Delta : AggregationTemporality.Cumulative, histogram.Histogram.AggregationTemporality);
        var hp = histogram.Histogram.DataPoints.Single();
        Assert.AreEqual(delta ? 2000UL : 1000UL, hp.StartTimeUnixNano);
        Assert.AreEqual(3000UL, hp.TimeUnixNano);
        Assert.AreEqual(delta ? 2UL : 5UL, hp.Count);
        Assert.IsTrue(hp.HasSum);
        Assert.AreEqual(delta ? 30.0 : 141.0, hp.Sum);
        CollectionAssert.AreEqual(delta ? new ulong[] { 0, 2, 0 } : new ulong[] { 1, 3, 1 }, hp.BucketCounts.ToArray());
        CollectionAssert.AreEqual(new[] { 10.0, 100.0 }, hp.ExplicitBounds.ToArray());
        Assert.IsTrue(hp.HasMin && hp.HasMax);
        Assert.AreEqual(delta ? 10.0 : 1.0, hp.Min);
        Assert.AreEqual(delta ? 20.0 : 100.0, hp.Max);
        Assert.AreEqual("GET", Attr(hp.Attributes, "http.request.method").StringValue);

        // A delta export leaves out points without measurements in the interval.
        Assert.AreEqual(!delta, metrics.ContainsKey("idle"));

        CollectionAssert.AreEqual(bytes, request.ToByteArray());
    }

    [TestMethod]
    public void Metrics_EmptyHistogramIntervalHasNoMinMax()
    {
        var metric = new BelemetryMetric
        {
            Name = "h",
            Type = BelemetryMetricType.Histogram,
            Points =
            {
                new BelemetryMetricPoint { StartTimeUnixNano = 1, TimeUnixNano = 2, Count = 0, BucketCounts = [0, 0], ExplicitBounds = [5] },
            },
        };

        var hp = ExportMetricsServiceRequest.Parser.ParseFrom(OtlpProtobufSerializer.SerializeMetrics(new BelemetryBatch(Records.Resource(), metrics: [metric]), BelemetryMetricTemporality.Cumulative))
            .ResourceMetrics[0].ScopeMetrics[0].Metrics[0].Histogram.DataPoints[0];

        Assert.AreEqual(0UL, hp.Count);
        Assert.IsFalse(hp.HasMin);
        Assert.IsFalse(hp.HasMax);
        CollectionAssert.AreEqual(new ulong[] { 0, 0 }, hp.BucketCounts.ToArray());
    }

    [TestMethod]
    public void Metrics_FromRealInstruments_Decode()
    {
        using var meter = new System.Diagnostics.Metrics.Meter("Conformance." + Guid.NewGuid().ToString("N"), "3.0");
        var settings = TestHost.CreateSettings(o => o.Metrics.Meters.Add(meter.Name));
        using var collector = new BelemetryMeterCollector(settings);

        var counter = meter.CreateCounter<long>("hits");
        var histogram = meter.CreateHistogram<double>("duration", "ms");
        counter.Add(3, new KeyValuePair<string, object?>("route", "/a"));
        histogram.Record(12.5);

        var bytes = OtlpProtobufSerializer.SerializeMetrics(new BelemetryBatch(Records.Resource(), metrics: collector.Collect()), BelemetryMetricTemporality.Cumulative);
        var metrics = ExportMetricsServiceRequest.Parser.ParseFrom(bytes).ResourceMetrics[0].ScopeMetrics[0].Metrics.ToDictionary(m => m.Name);

        Assert.AreEqual(3L, metrics["hits"].Sum.DataPoints[0].AsInt);
        Assert.AreEqual("/a", Attr(metrics["hits"].Sum.DataPoints[0].Attributes, "route").StringValue);
        Assert.AreEqual("ms", metrics["duration"].Unit);
        Assert.AreEqual(1UL, metrics["duration"].Histogram.DataPoints[0].Count);
        Assert.AreEqual(12.5, metrics["duration"].Histogram.DataPoints[0].Sum);
    }

    internal static List<BelemetryMetric> MetricsSample()
    {
        var scope = new BelemetryScope("MyApp.Meter", "1.0");
        return
        [
            new BelemetryMetric
            {
                Name = "queue.length", Description = "Items waiting", Unit = "{item}", Type = BelemetryMetricType.Gauge, Scope = scope,
                Points = { new BelemetryMetricPoint { Attributes = new BelemetryAttributes().Set("region", "eu"), StartTimeUnixNano = 1000, DeltaStartTimeUnixNano = 2000, TimeUnixNano = 3000, Value = 7.5, HasDeltaUpdate = false } },
            },
            new BelemetryMetric
            {
                Name = "requests", Type = BelemetryMetricType.Sum, IsMonotonic = true, Scope = scope,
                Points = { new BelemetryMetricPoint { StartTimeUnixNano = 1000, DeltaStartTimeUnixNano = 2000, TimeUnixNano = 3000, IsInteger = true, LongValue = 10, Value = 10, DeltaLongValue = 4, DeltaValue = 4, HasDeltaUpdate = true } },
            },
            new BelemetryMetric
            {
                Name = "balance", Type = BelemetryMetricType.Sum, IsMonotonic = false, Scope = scope,
                Points = { new BelemetryMetricPoint { StartTimeUnixNano = 1000, DeltaStartTimeUnixNano = 2000, TimeUnixNano = 3000, Value = 3.5, DeltaValue = -1.25, HasDeltaUpdate = true } },
            },
            new BelemetryMetric
            {
                Name = "latency", Unit = "ms", Type = BelemetryMetricType.Histogram, Scope = scope,
                Points =
                {
                    new BelemetryMetricPoint
                    {
                        Attributes = new BelemetryAttributes().Set("http.request.method", "GET"),
                        StartTimeUnixNano = 1000, DeltaStartTimeUnixNano = 2000, TimeUnixNano = 3000, HasDeltaUpdate = true,
                        Count = 5, Sum = 141, Min = 1, Max = 100, BucketCounts = [1, 3, 1],
                        DeltaCount = 2, DeltaSum = 30, DeltaMin = 10, DeltaMax = 20, DeltaBucketCounts = [0, 2, 0],
                        ExplicitBounds = [10, 100],
                    },
                },
            },
            new BelemetryMetric
            {
                Name = "idle", Type = BelemetryMetricType.Sum, IsMonotonic = true, Scope = scope,
                Points = { new BelemetryMetricPoint { StartTimeUnixNano = 1000, DeltaStartTimeUnixNano = 2000, TimeUnixNano = 3000, IsInteger = true, LongValue = 3, HasDeltaUpdate = false } },
            },
        ];
    }

    internal static AnyValue Attr(RepeatedField<KeyValue> attributes, string key)
    {
        var matches = attributes.Where(a => a.Key == key).ToList();
        Assert.HasCount(1, matches, $"Attribute '{key}'");
        return matches[0].Value;
    }
}
