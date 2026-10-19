using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Encodes batches as OTLP/JSON, following the OTLP spec's JSON rules: lowerCamelCase field names, trace and span ids
/// as hex strings (not base64), enums as integers, and 64-bit integers as decimal strings.
/// </summary>
/// <remarks>See <see href="https://opentelemetry.io/docs/specs/otlp/#json-protobuf-encoding"/>.</remarks>
internal static class OtlpJsonSerializer
{
    public static byte[] SerializeTraces(BelemetryBatch batch)
    {
        return Write(w =>
        {
            w.WriteStartObject();
            w.WriteStartArray("resourceSpans");
            w.WriteStartObject();
            WriteResource(w, batch.Resource);
            w.WriteStartArray("scopeSpans");
            foreach (var group in batch.Spans.GroupBy(s => s.Scope))
            {
                w.WriteStartObject();
                WriteScope(w, group.Key);
                w.WriteStartArray("spans");
                foreach (var span in group) WriteSpan(w, span);
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        });
    }

    public static byte[] SerializeLogs(BelemetryBatch batch)
    {
        return Write(w =>
        {
            w.WriteStartObject();
            w.WriteStartArray("resourceLogs");
            w.WriteStartObject();
            WriteResource(w, batch.Resource);
            w.WriteStartArray("scopeLogs");
            foreach (var group in batch.Logs.GroupBy(l => l.Scope))
            {
                w.WriteStartObject();
                WriteScope(w, group.Key);
                w.WriteStartArray("logRecords");
                foreach (var log in group) WriteLog(w, log);
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        });
    }

    public static byte[] SerializeMetrics(BelemetryBatch batch, BelemetryMetricTemporality temporality)
    {
        return Write(w =>
        {
            w.WriteStartObject();
            w.WriteStartArray("resourceMetrics");
            w.WriteStartObject();
            WriteResource(w, batch.Resource);
            w.WriteStartArray("scopeMetrics");
            foreach (var group in batch.Metrics.GroupBy(m => m.Scope))
            {
                w.WriteStartObject();
                WriteScope(w, group.Key);
                w.WriteStartArray("metrics");
                foreach (var metric in group) WriteMetric(w, metric, temporality);
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        });
    }

    private static byte[] Write(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(4096);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            write(writer);
        }
        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteResource(Utf8JsonWriter w, BelemetryResource resource)
    {
        w.WriteStartObject("resource");
        WriteAttributes(w, resource.Attributes);
        if (resource.Attributes.DroppedCount > 0) w.WriteNumber("droppedAttributesCount", resource.Attributes.DroppedCount);
        w.WriteEndObject();
    }

    private static void WriteScope(Utf8JsonWriter w, BelemetryScope scope)
    {
        w.WriteStartObject("scope");
        w.WriteString("name", scope.Name);
        if (string.IsNullOrEmpty(scope.Version) is false) w.WriteString("version", scope.Version);
        w.WriteEndObject();
    }

    private static void WriteSpan(Utf8JsonWriter w, BelemetrySpan span)
    {
        w.WriteStartObject();
        w.WriteString("traceId", span.TraceId);
        w.WriteString("spanId", span.SpanId);
        if (string.IsNullOrEmpty(span.TraceState) is false) w.WriteString("traceState", span.TraceState);
        if (span.ParentSpanId is not null) w.WriteString("parentSpanId", span.ParentSpanId);
        w.WriteNumber("flags", span.Flags & 0xFF);
        w.WriteString("name", span.Name);
        w.WriteNumber("kind", (int)span.Kind);
        WriteNano(w, "startTimeUnixNano", span.StartTimeUnixNano);
        WriteNano(w, "endTimeUnixNano", span.EndTimeUnixNano);
        WriteAttributes(w, span.Attributes);
        if (span.Attributes.DroppedCount > 0) w.WriteNumber("droppedAttributesCount", span.Attributes.DroppedCount);

        if (span.Events.Count > 0)
        {
            w.WriteStartArray("events");
            foreach (var e in span.Events)
            {
                w.WriteStartObject();
                WriteNano(w, "timeUnixNano", e.TimeUnixNano);
                w.WriteString("name", e.Name);
                WriteAttributes(w, e.Attributes);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }

        if (span.Links.Count > 0)
        {
            w.WriteStartArray("links");
            foreach (var link in span.Links)
            {
                w.WriteStartObject();
                w.WriteString("traceId", link.TraceId);
                w.WriteString("spanId", link.SpanId);
                if (string.IsNullOrEmpty(link.TraceState) is false) w.WriteString("traceState", link.TraceState);
                WriteAttributes(w, link.Attributes);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }

        if (span.Status != BelemetryStatusCode.Unset)
        {
            w.WriteStartObject("status");
            if (span.Status == BelemetryStatusCode.Error && string.IsNullOrEmpty(span.StatusMessage) is false) w.WriteString("message", span.StatusMessage);
            w.WriteNumber("code", (int)span.Status);
            w.WriteEndObject();
        }

        w.WriteEndObject();
    }

    private static void WriteLog(Utf8JsonWriter w, BelemetryLogRecord log)
    {
        w.WriteStartObject();
        WriteNano(w, "timeUnixNano", log.TimeUnixNano);
        if (log.ObservedTimeUnixNano != 0) WriteNano(w, "observedTimeUnixNano", log.ObservedTimeUnixNano);
        if (log.Severity != BelemetrySeverity.Unspecified) w.WriteNumber("severityNumber", (int)log.Severity);
        if (string.IsNullOrEmpty(log.SeverityText) is false) w.WriteString("severityText", log.SeverityText);
        if (log.Body is not null)
        {
            w.WriteStartObject("body");
            w.WriteString("stringValue", log.Body);
            w.WriteEndObject();
        }
        WriteAttributes(w, log.Attributes);
        if (log.Attributes.DroppedCount > 0) w.WriteNumber("droppedAttributesCount", log.Attributes.DroppedCount);
        if (log.Flags != 0) w.WriteNumber("flags", log.Flags & 0xFF);
        if (log.TraceId is not null) w.WriteString("traceId", log.TraceId);
        if (log.SpanId is not null) w.WriteString("spanId", log.SpanId);
        if (string.IsNullOrEmpty(log.EventName) is false) w.WriteString("eventName", log.EventName);
        w.WriteEndObject();
    }

    private static void WriteMetric(Utf8JsonWriter w, BelemetryMetric metric, BelemetryMetricTemporality temporality)
    {
        var points = OtlpMetricPoints.Select(metric, temporality);
        if (points.Count == 0) return;

        w.WriteStartObject();
        w.WriteString("name", metric.Name);
        if (string.IsNullOrEmpty(metric.Description) is false) w.WriteString("description", metric.Description);
        if (string.IsNullOrEmpty(metric.Unit) is false) w.WriteString("unit", metric.Unit);

        switch (metric.Type)
        {
            case BelemetryMetricType.Gauge:
                w.WriteStartObject("gauge");
                w.WriteStartArray("dataPoints");
                foreach (var p in points) WriteNumberPoint(w, p, metric, temporality);
                w.WriteEndArray();
                w.WriteEndObject();
                break;

            case BelemetryMetricType.Sum:
                w.WriteStartObject("sum");
                w.WriteStartArray("dataPoints");
                foreach (var p in points) WriteNumberPoint(w, p, metric, temporality);
                w.WriteEndArray();
                w.WriteNumber("aggregationTemporality", (int)temporality);
                w.WriteBoolean("isMonotonic", metric.IsMonotonic);
                w.WriteEndObject();
                break;

            case BelemetryMetricType.Histogram:
                w.WriteStartObject("histogram");
                w.WriteStartArray("dataPoints");
                foreach (var p in points) WriteHistogramPoint(w, p, temporality);
                w.WriteEndArray();
                w.WriteNumber("aggregationTemporality", (int)temporality);
                w.WriteEndObject();
                break;
        }

        w.WriteEndObject();
    }

    private static void WriteNumberPoint(Utf8JsonWriter w, BelemetryMetricPoint p, BelemetryMetric metric, BelemetryMetricTemporality temporality)
    {
        var delta = temporality == BelemetryMetricTemporality.Delta && metric.Type == BelemetryMetricType.Sum;

        w.WriteStartObject();
        WriteAttributes(w, p.Attributes);
        if (metric.Type != BelemetryMetricType.Gauge) WriteNano(w, "startTimeUnixNano", delta ? p.DeltaStartTimeUnixNano : p.StartTimeUnixNano);
        WriteNano(w, "timeUnixNano", p.TimeUnixNano);
        if (p.IsInteger) w.WriteString("asInt", (delta ? p.DeltaLongValue : p.LongValue).ToString(CultureInfo.InvariantCulture));
        else WriteDouble(w, "asDouble", delta ? p.DeltaValue : p.Value);
        w.WriteEndObject();
    }

    private static void WriteHistogramPoint(Utf8JsonWriter w, BelemetryMetricPoint p, BelemetryMetricTemporality temporality)
    {
        var delta = temporality == BelemetryMetricTemporality.Delta;
        var count = delta ? p.DeltaCount : p.Count;

        w.WriteStartObject();
        WriteAttributes(w, p.Attributes);
        WriteNano(w, "startTimeUnixNano", delta ? p.DeltaStartTimeUnixNano : p.StartTimeUnixNano);
        WriteNano(w, "timeUnixNano", p.TimeUnixNano);
        w.WriteString("count", count.ToString(CultureInfo.InvariantCulture));
        WriteDouble(w, "sum", delta ? p.DeltaSum : p.Sum);

        w.WriteStartArray("bucketCounts");
        foreach (var c in delta ? p.DeltaBucketCounts : p.BucketCounts) w.WriteStringValue(c.ToString(CultureInfo.InvariantCulture));
        w.WriteEndArray();

        w.WriteStartArray("explicitBounds");
        foreach (var b in p.ExplicitBounds) w.WriteNumberValue(b);
        w.WriteEndArray();

        if (count > 0)
        {
            WriteDouble(w, "min", delta ? p.DeltaMin : p.Min);
            WriteDouble(w, "max", delta ? p.DeltaMax : p.Max);
        }
        w.WriteEndObject();
    }

    private static void WriteAttributes(Utf8JsonWriter w, BelemetryAttributes attributes)
    {
        w.WriteStartArray("attributes");
        foreach (var (key, value) in attributes)
        {
            w.WriteStartObject();
            w.WriteString("key", key);
            w.WritePropertyName("value");
            WriteAnyValue(w, value);
            w.WriteEndObject();
        }
        w.WriteEndArray();
    }

    private static void WriteAnyValue(Utf8JsonWriter w, object value)
    {
        w.WriteStartObject();
        switch (value)
        {
            case string s: w.WriteString("stringValue", s); break;
            case bool b: w.WriteBoolean("boolValue", b); break;
            case long l: w.WriteString("intValue", l.ToString(CultureInfo.InvariantCulture)); break;
            case double d: WriteDouble(w, "doubleValue", d); break;
            case byte[] bytes: w.WriteBase64String("bytesValue", bytes); break;
            case string[] a: WriteArray(w, a.Cast<object>()); break;
            case long[] a: WriteArray(w, a.Cast<object>()); break;
            case double[] a: WriteArray(w, a.Cast<object>()); break;
            case bool[] a: WriteArray(w, a.Cast<object>()); break;
            default: w.WriteString("stringValue", BelemetryAttributes.ToInvariantString(value)); break;
        }
        w.WriteEndObject();
    }

    private static void WriteArray(Utf8JsonWriter w, IEnumerable<object> values)
    {
        w.WriteStartObject("arrayValue");
        w.WriteStartArray("values");
        foreach (var value in values) WriteAnyValue(w, value);
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WriteNano(Utf8JsonWriter w, string name, long value) => w.WriteString(name, value.ToString(CultureInfo.InvariantCulture));

    private static void WriteDouble(Utf8JsonWriter w, string name, double value)
    {
        // JSON has no NaN/Infinity; the proto3 JSON mapping spells them as strings.
        if (double.IsNaN(value)) w.WriteString(name, "NaN");
        else if (double.IsPositiveInfinity(value)) w.WriteString(name, "Infinity");
        else if (double.IsNegativeInfinity(value)) w.WriteString(name, "-Infinity");
        else w.WriteNumber(name, value);
    }
}
