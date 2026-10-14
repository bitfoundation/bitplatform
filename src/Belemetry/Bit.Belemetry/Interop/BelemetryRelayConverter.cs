using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>Converts relayed browser records into the .NET model.</summary>
internal static class BelemetryRelayConverter
{
    private const int MaxRecordsPerBatch = 5000;

    public static IEnumerable<BelemetrySpan> ToSpans(BelemetryRelayBatch batch)
    {
        if (batch.Spans is null) yield break;

        foreach (var s in batch.Spans.Take(MaxRecordsPerBatch))
        {
            // Ids come from the page, so they are validated rather than trusted: a malformed id would make the
            // whole OTLP request invalid for the backend.
            if (BelemetryHex.IsValidId(s.TraceId, 16) is false || BelemetryHex.IsValidId(s.SpanId, 8) is false) continue;

            var span = new BelemetrySpan
            {
                TraceId = s.TraceId!.ToLowerInvariant(),
                SpanId = s.SpanId!.ToLowerInvariant(),
                ParentSpanId = BelemetryHex.IsValidId(s.ParentSpanId, 8) ? s.ParentSpanId!.ToLowerInvariant() : null,
                Name = string.IsNullOrEmpty(s.Name) ? "span" : s.Name,
                Kind = s.Kind is >= 0 and <= 5 ? (BelemetrySpanKind)s.Kind : BelemetrySpanKind.Internal,
                StartTimeUnixNano = BelemetryTime.FromUnixMilliseconds(s.StartTime),
                EndTimeUnixNano = BelemetryTime.FromUnixMilliseconds(Math.Max(s.StartTime, s.EndTime)),
                Status = s.Status?.Code is >= 0 and <= 2 ? (BelemetryStatusCode)s.Status.Code : BelemetryStatusCode.Unset,
                StatusMessage = s.Status?.Message,
                Scope = new BelemetryScope(string.IsNullOrEmpty(s.Scope) ? "bit.belemetry.browser" : s.Scope, null),
                Feature = ToFeature(s.Feature, BelemetryFeatures.Dependencies),
            };

            Fill(span.Attributes, s.Attributes);

            if (s.Events is not null)
            {
                foreach (var e in s.Events.Take(128))
                {
                    var spanEvent = new BelemetrySpanEvent { Name = e.Name ?? "event", TimeUnixNano = BelemetryTime.FromUnixMilliseconds(e.Time) };
                    Fill(spanEvent.Attributes, e.Attributes);
                    span.Events.Add(spanEvent);
                }
            }

            yield return span;
        }
    }

    public static IEnumerable<BelemetryLogRecord> ToLogs(BelemetryRelayBatch batch)
    {
        if (batch.Logs is null) yield break;

        var observed = BelemetryTime.NowUnixNano();
        foreach (var l in batch.Logs.Take(MaxRecordsPerBatch))
        {
            var log = new BelemetryLogRecord
            {
                TimeUnixNano = BelemetryTime.FromUnixMilliseconds(l.Time),
                ObservedTimeUnixNano = observed,
                Severity = (BelemetrySeverity)Math.Clamp(l.SeverityNumber, 0, 24),
                SeverityText = l.SeverityText,
                Body = l.Body,
                EventName = l.EventName,
                TraceId = BelemetryHex.IsValidId(l.TraceId, 16) ? l.TraceId!.ToLowerInvariant() : null,
                SpanId = BelemetryHex.IsValidId(l.SpanId, 8) ? l.SpanId!.ToLowerInvariant() : null,
                Scope = new BelemetryScope(string.IsNullOrEmpty(l.Scope) ? "bit.belemetry.browser" : l.Scope, null),
                Feature = ToFeature(l.Feature, BelemetryFeatures.Logs),
            };

            Fill(log.Attributes, l.Attributes);
            yield return log;
        }
    }

    public static BelemetryAttributes ToAttributes(Dictionary<string, JsonElement>? values)
    {
        var attributes = new BelemetryAttributes();
        Fill(attributes, values);
        return attributes;
    }

    private static BelemetryFeatures ToFeature(int value, BelemetryFeatures fallback)
    {
        // Exactly one known bit, as the contract promises; anything else falls back.
        var feature = (BelemetryFeatures)value;
        return value > 0 && (value & (value - 1)) == 0 && (feature & BelemetryFeatures.All) == feature ? feature : fallback;
    }

    private static void Fill(BelemetryAttributes target, Dictionary<string, JsonElement>? values)
    {
        if (values is null) return;
        foreach (var (key, value) in values) target.Set(key, ToValue(value));
    }

    internal static object? ToValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
                return value.TryGetInt64(out var l) ? l : value.GetDouble();
            case JsonValueKind.Array:
                {
                    var items = value.EnumerateArray().Select(ToValue).Where(v => v is not null).ToList();
                    if (items.Count == 0) return Array.Empty<string>();
                    if (items.All(v => v is long)) return items.Cast<long>().ToArray();
                    if (items.All(v => v is long or double)) return items.Select(v => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    if (items.All(v => v is bool)) return items.Cast<bool>().ToArray();
                    return items.Select(BelemetryAttributes.ToInvariantString).ToArray();
                }
            case JsonValueKind.Object:
                return value.GetRawText();
            default:
                return null;
        }
    }
}
