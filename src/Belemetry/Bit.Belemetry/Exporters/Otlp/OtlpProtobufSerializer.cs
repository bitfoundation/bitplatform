namespace Bit.Belemetry;

/// <summary>
/// Encodes batches as OTLP <c>Export{Trace,Logs,Metrics}ServiceRequest</c> protobuf messages. Field numbers follow
/// <c>opentelemetry-proto</c> v1.x (verified against v1.11.1); the tests decode the output with types generated from
/// those official <c>.proto</c> files.
/// </summary>
internal static class OtlpProtobufSerializer
{
    public static byte[] SerializeTraces(BelemetryBatch batch)
    {
        var w = new ProtobufWriter();
        var resourceSpans = w.BeginMessage(1);
        WriteResource(w, 1, batch.Resource);

        foreach (var group in batch.Spans.GroupBy(s => s.Scope))
        {
            var scopeSpans = w.BeginMessage(2);
            WriteScope(w, 1, group.Key);
            foreach (var span in group) WriteSpan(w, span);
            w.EndMessage(scopeSpans);
        }

        w.EndMessage(resourceSpans);
        return w.ToArray();
    }

    public static byte[] SerializeLogs(BelemetryBatch batch)
    {
        var w = new ProtobufWriter();
        var resourceLogs = w.BeginMessage(1);
        WriteResource(w, 1, batch.Resource);

        foreach (var group in batch.Logs.GroupBy(l => l.Scope))
        {
            var scopeLogs = w.BeginMessage(2);
            WriteScope(w, 1, group.Key);
            foreach (var log in group) WriteLog(w, log);
            w.EndMessage(scopeLogs);
        }

        w.EndMessage(resourceLogs);
        return w.ToArray();
    }

    public static byte[] SerializeMetrics(BelemetryBatch batch, BelemetryMetricTemporality temporality)
    {
        var w = new ProtobufWriter();
        var resourceMetrics = w.BeginMessage(1);
        WriteResource(w, 1, batch.Resource);

        foreach (var group in batch.Metrics.GroupBy(m => m.Scope))
        {
            var scopeMetrics = w.BeginMessage(2);
            WriteScope(w, 1, group.Key);
            foreach (var metric in group) WriteMetric(w, metric, temporality);
            w.EndMessage(scopeMetrics);
        }

        w.EndMessage(resourceMetrics);
        return w.ToArray();
    }

    private static void WriteResource(ProtobufWriter w, int field, BelemetryResource resource)
    {
        var token = w.BeginMessage(field);
        WriteAttributes(w, 1, resource.Attributes);
        if (resource.Attributes.DroppedCount > 0) w.WriteVarintField(2, (ulong)resource.Attributes.DroppedCount);
        w.EndMessage(token);
    }

    private static void WriteScope(ProtobufWriter w, int field, BelemetryScope scope)
    {
        var token = w.BeginMessage(field);
        w.WriteStringIfNotEmpty(1, scope.Name);
        w.WriteStringIfNotEmpty(2, scope.Version);
        w.EndMessage(token);
    }

    private static void WriteSpan(ProtobufWriter w, BelemetrySpan span)
    {
        var token = w.BeginMessage(2);
        w.WriteHexId(1, span.TraceId, 16);
        w.WriteHexId(2, span.SpanId, 8);
        w.WriteStringIfNotEmpty(3, span.TraceState);
        if (span.ParentSpanId is not null) w.WriteHexId(4, span.ParentSpanId, 8);
        w.WriteString(5, span.Name);
        w.WriteInt64(6, (int)span.Kind);
        w.WriteFixed64(7, (ulong)span.StartTimeUnixNano);
        w.WriteFixed64(8, (ulong)span.EndTimeUnixNano);
        WriteAttributes(w, 9, span.Attributes);
        if (span.Attributes.DroppedCount > 0) w.WriteVarintField(10, (ulong)span.Attributes.DroppedCount);

        foreach (var e in span.Events)
        {
            var eventToken = w.BeginMessage(11);
            w.WriteFixed64(1, (ulong)e.TimeUnixNano);
            w.WriteString(2, e.Name);
            WriteAttributes(w, 3, e.Attributes);
            w.EndMessage(eventToken);
        }

        foreach (var link in span.Links)
        {
            var linkToken = w.BeginMessage(13);
            w.WriteHexId(1, link.TraceId, 16);
            w.WriteHexId(2, link.SpanId, 8);
            w.WriteStringIfNotEmpty(3, link.TraceState);
            WriteAttributes(w, 4, link.Attributes);
            w.EndMessage(linkToken);
        }

        if (span.Status != BelemetryStatusCode.Unset || span.StatusMessage is not null)
        {
            var statusToken = w.BeginMessage(15);
            if (span.Status == BelemetryStatusCode.Error) w.WriteStringIfNotEmpty(2, span.StatusMessage);
            if (span.Status != BelemetryStatusCode.Unset) w.WriteInt64(3, (int)span.Status);
            w.EndMessage(statusToken);
        }

        w.WriteFixed32(16, (uint)(span.Flags & 0xFF));
        w.EndMessage(token);
    }

    private static void WriteLog(ProtobufWriter w, BelemetryLogRecord log)
    {
        var token = w.BeginMessage(2);
        w.WriteFixed64(1, (ulong)log.TimeUnixNano);
        if (log.Severity != BelemetrySeverity.Unspecified) w.WriteInt64(2, (int)log.Severity);
        w.WriteStringIfNotEmpty(3, log.SeverityText);

        if (log.Body is not null)
        {
            var body = w.BeginMessage(5);
            w.WriteString(1, log.Body);
            w.EndMessage(body);
        }

        WriteAttributes(w, 6, log.Attributes);
        if (log.Attributes.DroppedCount > 0) w.WriteVarintField(7, (ulong)log.Attributes.DroppedCount);
        if (log.Flags != 0) w.WriteFixed32(8, (uint)(log.Flags & 0xFF));
        w.WriteHexId(9, log.TraceId, 16);
        w.WriteHexId(10, log.SpanId, 8);
        if (log.ObservedTimeUnixNano != 0) w.WriteFixed64(11, (ulong)log.ObservedTimeUnixNano);
        w.WriteStringIfNotEmpty(12, log.EventName);
        w.EndMessage(token);
    }

    private static void WriteMetric(ProtobufWriter w, BelemetryMetric metric, BelemetryMetricTemporality temporality)
    {
        var points = OtlpMetricPoints.Select(metric, temporality);
        if (points.Count == 0) return;

        var token = w.BeginMessage(2);
        w.WriteString(1, metric.Name);
        w.WriteStringIfNotEmpty(2, metric.Description);
        w.WriteStringIfNotEmpty(3, metric.Unit);

        switch (metric.Type)
        {
            case BelemetryMetricType.Gauge:
                {
                    var gauge = w.BeginMessage(5);
                    foreach (var p in points) WriteNumberPoint(w, p, metric, temporality);
                    w.EndMessage(gauge);
                    break;
                }
            case BelemetryMetricType.Sum:
                {
                    var sum = w.BeginMessage(7);
                    foreach (var p in points) WriteNumberPoint(w, p, metric, temporality);
                    w.WriteInt64(2, (int)temporality);
                    if (metric.IsMonotonic) w.WriteBool(3, true);
                    w.EndMessage(sum);
                    break;
                }
            case BelemetryMetricType.Histogram:
                {
                    var histogram = w.BeginMessage(9);
                    foreach (var p in points) WriteHistogramPoint(w, p, temporality);
                    w.WriteInt64(2, (int)temporality);
                    w.EndMessage(histogram);
                    break;
                }
        }

        w.EndMessage(token);
    }

    private static void WriteNumberPoint(ProtobufWriter w, BelemetryMetricPoint p, BelemetryMetric metric, BelemetryMetricTemporality temporality)
    {
        var delta = temporality == BelemetryMetricTemporality.Delta && metric.Type == BelemetryMetricType.Sum;

        var token = w.BeginMessage(1);
        if (metric.Type != BelemetryMetricType.Gauge) w.WriteFixed64(2, (ulong)(delta ? p.DeltaStartTimeUnixNano : p.StartTimeUnixNano));
        w.WriteFixed64(3, (ulong)p.TimeUnixNano);

        if (p.IsInteger) w.WriteSFixed64(6, delta ? p.DeltaLongValue : p.LongValue);
        else w.WriteDouble(4, delta ? p.DeltaValue : p.Value);

        WriteAttributes(w, 7, p.Attributes);
        w.EndMessage(token);
    }

    private static void WriteHistogramPoint(ProtobufWriter w, BelemetryMetricPoint p, BelemetryMetricTemporality temporality)
    {
        var delta = temporality == BelemetryMetricTemporality.Delta;
        var count = delta ? p.DeltaCount : p.Count;

        var token = w.BeginMessage(1);
        w.WriteFixed64(2, (ulong)(delta ? p.DeltaStartTimeUnixNano : p.StartTimeUnixNano));
        w.WriteFixed64(3, (ulong)p.TimeUnixNano);
        w.WriteFixed64(4, (ulong)count);
        w.WriteDouble(5, delta ? p.DeltaSum : p.Sum);
        w.WritePackedFixed64(6, delta ? p.DeltaBucketCounts : p.BucketCounts);
        w.WritePackedDouble(7, p.ExplicitBounds);
        WriteAttributes(w, 9, p.Attributes);

        if (count > 0)
        {
            w.WriteDouble(11, delta ? p.DeltaMin : p.Min);
            w.WriteDouble(12, delta ? p.DeltaMax : p.Max);
        }

        w.EndMessage(token);
    }

    private static void WriteAttributes(ProtobufWriter w, int field, BelemetryAttributes attributes)
    {
        foreach (var (key, value) in attributes)
        {
            var kv = w.BeginMessage(field);
            w.WriteString(1, key);
            var any = w.BeginMessage(2);
            WriteAnyValue(w, value);
            w.EndMessage(any);
            w.EndMessage(kv);
        }
    }

    private static void WriteAnyValue(ProtobufWriter w, object value)
    {
        switch (value)
        {
            case string s: w.WriteString(1, s); break;
            case bool b: w.WriteBool(2, b); break;
            case long l: w.WriteInt64(3, l); break;
            case double d: w.WriteDouble(4, d); break;
            case byte[] bytes: w.WriteBytes(7, bytes); break;
            case string[] a: WriteArray(w, a, static (w, v) => w.WriteString(1, v)); break;
            case long[] a: WriteArray(w, a, static (w, v) => w.WriteInt64(3, v)); break;
            case double[] a: WriteArray(w, a, static (w, v) => w.WriteDouble(4, v)); break;
            case bool[] a: WriteArray(w, a, static (w, v) => w.WriteBool(2, v)); break;
            default: w.WriteString(1, BelemetryAttributes.ToInvariantString(value)); break;
        }
    }

    private static void WriteArray<T>(ProtobufWriter w, T[] values, Action<ProtobufWriter, T> writeValue)
    {
        var array = w.BeginMessage(5);
        foreach (var value in values)
        {
            var any = w.BeginMessage(1);
            writeValue(w, value);
            w.EndMessage(any);
        }
        w.EndMessage(array);
    }
}
