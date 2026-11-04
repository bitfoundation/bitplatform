using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Reads the browser script's diagnostic timeline (<c>getDiagnostics()</c>). On a server the page is untrusted - any
/// client of a circuit can answer anything - so every event is bounded and sanitized again here, exceptions are
/// fingerprinted here (with the same algorithm as .NET's), and only the attributes a category keeps survive.
/// </summary>
internal static class BelemetryBrowserEvidence
{
    private const int AbsoluteMaxEvents = 1000;

    private static readonly HashSet<string> _keptAttributes = new(StringComparer.Ordinal)
    {
        BelemetrySemanticConventions.HttpRequestMethod, BelemetrySemanticConventions.HttpResponseStatusCode, BelemetrySemanticConventions.ServerAddress,
        BelemetrySemanticConventions.ServerPort, BelemetrySemanticConventions.ErrorType, "browser.navigation.type", "event.type", "target.element",
        "visibility.state", "reconnect.state", "reconnect.attempt", "longtask.duration",
    };

    public sealed class Result
    {
        public List<BelemetryDiagnosticEvent> Events { get; } = [];

        public Dictionary<string, string> Browser { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> Dropped { get; } = new(StringComparer.Ordinal);
    }

    public static Result Read(JsonElement root, BelemetryDiagnosticHub hub)
    {
        var result = new Result();
        if (root.ValueKind != JsonValueKind.Object) return result;

        var max = Math.Min(AbsoluteMaxEvents, Math.Max(1, hub.Options.BrowserMaxEvents) * 2);
        var sanitizer = hub.Sanitizer;

        if (root.TryGetProperty("dropped", out var dropped) && dropped.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in dropped.EnumerateObject().Take(16))
            {
                if (property.Value.TryGetInt32(out var count) && count > 0) result.Dropped["browser." + Safe(property.Name, 32)] = count;
            }
        }

        if (root.TryGetProperty("resource", out var resource) && resource.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "browser.brands", "browser.platform", "browser.mobile", "browser.language" })
            {
                if (resource.TryGetProperty(key, out var value))
                {
                    var counters = new BelemetrySanitizeCounters();
                    var text = value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Array => string.Join(", ", value.EnumerateArray().Take(8).Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString())),
                        _ => null,
                    };
                    if (sanitizer.Text(text, 256, ref counters) is { } safe) result.Browser[key] = safe;
                }
            }
        }

        if (root.TryGetProperty("events", out var events) is false || events.ValueKind != JsonValueKind.Array) return result;

        var malformed = 0;
        foreach (var item in events.EnumerateArray())
        {
            if (result.Events.Count >= max)
            {
                malformed++;
                continue;
            }

            try
            {
                if (ReadEvent(item, hub) is { } e) result.Events.Add(e);
                else malformed++;
            }
            catch
            {
                malformed++;
            }
        }
        if (malformed > 0) result.Dropped["browser.malformed"] = malformed;
        return result;
    }

    private static BelemetryDiagnosticEvent? ReadEvent(JsonElement e, BelemetryDiagnosticHub hub)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;

        var sanitizer = hub.Sanitizer;
        var counters = new BelemetrySanitizeCounters();
        var category = BelemetryDiagnosticJson.ParseCategory(BelemetryDiagnosticJson.String(e, "cat"));
        if ((hub.Options.Categories & category) == 0) return null;

        var time = BelemetryDiagnosticJson.Double(e, "time");
        if (time is null or <= 0) return null;

        BelemetryDiagnosticException? exception = null;
        if (e.TryGetProperty("exc", out var exc) && exc.ValueKind == JsonValueKind.Object)
        {
            exception = hub.Factory.FromText(BelemetryDiagnosticJson.String(exc, "type"), BelemetryDiagnosticJson.String(exc, "message"), BelemetryDiagnosticJson.String(exc, "stack"), "browser", ref counters);
            if (exception is null) return null; // Ignored by a classifier.
        }

        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (e.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in attrs.EnumerateObject().Take(32))
            {
                var key = property.Name;
                if (key == "url.template")
                {
                    if (property.Value.ValueKind == JsonValueKind.String && sanitizer.Url(property.Value.GetString(), ref counters) is { } url) attributes[key] = url;
                    continue;
                }
                if (_keptAttributes.Contains(key) is false && sanitizer.IsAllowedAttribute(key) is false) continue;

                object? value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var l) ? l : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null,
                };
                if (sanitizer.Scalar(key, value, ref counters, 256) is { } kept) attributes[key] = kept;
            }
        }

        var route = BelemetryDiagnosticJson.String(e, "route");
        var traceId = BelemetryDiagnosticJson.String(e, "traceId");
        var spanId = BelemetryDiagnosticJson.String(e, "spanId");
        var parentSpanId = BelemetryDiagnosticJson.String(e, "parentSpanId");
        var name = BelemetryDiagnosticJson.String(e, "name") ?? "event";

        return new BelemetryDiagnosticEvent
        {
            Sequence = BelemetryDiagnosticJson.Long(e, "seq") ?? 0,
            TimeUnixNano = BelemetryTime.FromUnixMilliseconds(time.Value),
            ElapsedMilliseconds = BelemetryDiagnosticJson.Double(e, "elapsed"),
            Category = category,
            Name = sanitizer.Text(Safe(name, 64), 64, ref counters) ?? "event",
            Severity = SeverityOf(BelemetryDiagnosticJson.Int(e, "sev")),
            Outcome = BelemetryDiagnosticJson.ParseOutcome(BelemetryDiagnosticJson.String(e, "outcome")),
            Source = BelemetryDiagnosticSource.Browser,
            Feature = (BelemetryFeatures)(BelemetryDiagnosticJson.Int(e, "feature") ?? 0) & BelemetryFeatures.All,
            TraceId = IsHex(traceId, 32) ? traceId : null,
            SpanId = IsHex(spanId, 16) ? spanId : null,
            ParentSpanId = IsHex(parentSpanId, 16) ? parentSpanId : null,
            // The page templated it; templated again here, as the page may be anything.
            Route = route is null ? null : sanitizer.Text(BelemetryUrlTemplate.TemplatePath(route), 512, ref counters),
            RouteSource = route is null ? null : "inferred",
            DurationMilliseconds = BelemetryDiagnosticJson.Double(e, "dur") is { } d && d >= 0 ? d : null,
            Exception = exception,
            Message = sanitizer.Text(BelemetryDiagnosticJson.String(e, "msg"), 512, ref counters),
            Attributes = attributes,
            Redactions = counters.Redactions,
            Truncated = counters.Truncations > 0,
            PreviousPage = e.TryGetProperty("prev", out var previous) && previous.ValueKind == JsonValueKind.True,
        };
    }

    private static BelemetrySeverity SeverityOf(int? number) => number switch
    {
        >= 21 => BelemetrySeverity.Fatal,
        >= 17 => BelemetrySeverity.Error,
        >= 13 => BelemetrySeverity.Warn,
        >= 9 => BelemetrySeverity.Info,
        >= 5 => BelemetrySeverity.Debug,
        _ => BelemetrySeverity.Info,
    };

    private static bool IsHex(string? value, int length) => value is not null && value.Length == length && value.All(char.IsAsciiHexDigitLower);

    private static string Safe(string value, int max)
    {
        var chars = value.Take(max).Select(c => char.IsControl(c) ? '?' : c).ToArray();
        return new string(chars);
    }
}
