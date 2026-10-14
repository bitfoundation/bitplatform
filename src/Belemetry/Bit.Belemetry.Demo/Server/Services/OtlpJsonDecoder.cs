using System.Globalization;
using System.Text;
using System.Text.Json;
using Bit.Belemetry.Demo.Client.Services;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// Decodes OTLP/HTTP JSON export requests (<c>ExportTraceServiceRequest</c>, <c>ExportLogsServiceRequest</c>,
/// <c>ExportMetricsServiceRequest</c>) into the flat <see cref="TelemetryRecord"/>s the live feed shows.
/// </summary>
/// <remarks>
/// Written against the OTLP JSON mapping (https://opentelemetry.io/docs/specs/otlp/#json-protobuf-encoding)
/// rather than against Belemetry's own serializer, so it reads anything that speaks OTLP/JSON: 64-bit
/// integers may arrive as strings or numbers, enums as integers or their names, and unknown fields are
/// ignored. It is a demo receiver, not a collector - what it cannot read it skips.
/// </remarks>
public static class OtlpJsonDecoder
{
    /// <summary>The longest attribute value kept; the viewer is for reading, not for archiving.</summary>
    public const int MaxValueLength = 2048;

    private static readonly string[] _spanKinds = ["Unspecified", "Internal", "Server", "Client", "Producer", "Consumer"];

    private static readonly string[] _spanKindNames =
        ["SPAN_KIND_UNSPECIFIED", "SPAN_KIND_INTERNAL", "SPAN_KIND_SERVER", "SPAN_KIND_CLIENT", "SPAN_KIND_PRODUCER", "SPAN_KIND_CONSUMER"];

    public static List<TelemetryRecord> DecodeTraces(JsonElement root)
    {
        var records = new List<TelemetryRecord>();

        foreach (var resourceSpans in Array(root, "resourceSpans"))
        {
            var resource = Attributes(resourceSpans.TryGetProperty("resource", out var r) ? r : default);
            var source = SourceOf(resource);

            foreach (var scopeSpans in Array(resourceSpans, "scopeSpans"))
            {
                var scope = ScopeName(scopeSpans);

                foreach (var span in Array(scopeSpans, "spans"))
                {
                    var attributes = Attributes(span);
                    var start = Nanos(span, "startTimeUnixNano");
                    var end = Nanos(span, "endTimeUnixNano");
                    var status = span.TryGetProperty("status", out var s) ? StatusOf(s) : null;

                    records.Add(new TelemetryRecord
                    {
                        Signal = "span",
                        Kind = "Span",
                        Source = source,
                        Time = start / 1_000_000d,
                        Name = Text(span, "name") ?? "(unnamed span)",
                        Scope = scope,
                        TraceId = Id(span, "traceId"),
                        SpanId = Id(span, "spanId"),
                        ParentSpanId = Id(span, "parentSpanId"),
                        SpanKind = SpanKindOf(span),
                        DurationMs = end > start ? (end - start) / 1_000_000d : 0,
                        Status = status,
                        SessionId = SessionOf(attributes, resource),
                        Attributes = attributes,
                        Resource = resource,
                        Events = [.. Array(span, "events").Select(e => Text(e, "name") ?? "event")],
                    });
                }
            }
        }

        return records;
    }

    public static List<TelemetryRecord> DecodeLogs(JsonElement root)
    {
        var records = new List<TelemetryRecord>();

        foreach (var resourceLogs in Array(root, "resourceLogs"))
        {
            var resource = Attributes(resourceLogs.TryGetProperty("resource", out var r) ? r : default);
            var source = SourceOf(resource);

            foreach (var scopeLogs in Array(resourceLogs, "scopeLogs"))
            {
                var scope = ScopeName(scopeLogs);

                foreach (var log in Array(scopeLogs, "logRecords"))
                {
                    var attributes = Attributes(log);
                    var severityNumber = Int(log, "severityNumber");
                    var body = log.TryGetProperty("body", out var b) ? AnyValue(b) : null;
                    var eventName = Text(log, "eventName") ?? attributes.GetValueOrDefault("event.name");
                    var time = Nanos(log, "timeUnixNano");
                    if (time == 0) time = Nanos(log, "observedTimeUnixNano");

                    var isError = severityNumber >= 17 || attributes.ContainsKey("exception.type") || attributes.ContainsKey("exception.message");

                    records.Add(new TelemetryRecord
                    {
                        Signal = "log",
                        Kind = isError ? "Error" : string.IsNullOrEmpty(eventName) ? "Log" : "Event",
                        Source = source,
                        Time = time / 1_000_000d,
                        Name = NameOfLog(eventName, body, attributes),
                        Body = body,
                        Severity = Text(log, "severityText") ?? (severityNumber > 0 ? SeverityName(severityNumber) : null),
                        Scope = scope,
                        TraceId = Id(log, "traceId"),
                        SpanId = Id(log, "spanId"),
                        SessionId = SessionOf(attributes, resource),
                        Attributes = attributes,
                        Resource = resource,
                    });
                }
            }
        }

        return records;
    }

    public static List<TelemetryRecord> DecodeMetrics(JsonElement root)
    {
        var records = new List<TelemetryRecord>();

        foreach (var resourceMetrics in Array(root, "resourceMetrics"))
        {
            var resource = Attributes(resourceMetrics.TryGetProperty("resource", out var r) ? r : default);
            var source = SourceOf(resource);

            foreach (var scopeMetrics in Array(resourceMetrics, "scopeMetrics"))
            {
                var scope = ScopeName(scopeMetrics);

                foreach (var metric in Array(scopeMetrics, "metrics"))
                {
                    var (type, points) = metric.TryGetProperty("gauge", out var g) ? ("gauge", g)
                                       : metric.TryGetProperty("sum", out var s) ? ("sum", s)
                                       : metric.TryGetProperty("histogram", out var h) ? ("histogram", h)
                                       : metric.TryGetProperty("exponentialHistogram", out var e) ? ("exponentialHistogram", e)
                                       : ("unknown", default);

                    var dataPoints = Array(points, "dataPoints").ToArray();
                    var time = dataPoints.Length > 0 ? Nanos(dataPoints[^1], "timeUnixNano") : 0;

                    var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["metric.type"] = type,
                        ["metric.points"] = dataPoints.Length.ToString(CultureInfo.InvariantCulture),
                    };
                    if (Text(metric, "unit") is { Length: > 0 } unit) attributes["metric.unit"] = unit;
                    if (Text(metric, "description") is { Length: > 0 } description) attributes["metric.description"] = description;
                    if (type == "sum") attributes["metric.monotonic"] = points.TryGetProperty("isMonotonic", out var mono) && mono.ValueKind == JsonValueKind.True ? "true" : "false";
                    if (points.ValueKind == JsonValueKind.Object && Int(points, "aggregationTemporality") is var temporality and > 0)
                    {
                        attributes["metric.temporality"] = temporality == 1 ? "delta" : "cumulative";
                    }

                    // The points' own attribute sets, one line each, so a counter by button reads as such.
                    for (var i = 0; i < Math.Min(dataPoints.Length, 20); i++)
                    {
                        var pointAttributes = Attributes(dataPoints[i]);
                        var label = pointAttributes.Count == 0 ? "(no attributes)" : string.Join(", ", pointAttributes.Select(a => $"{a.Key}={a.Value}"));
                        attributes[$"point[{i}]"] = $"{label}: {PointValue(type, dataPoints[i])}";
                    }

                    records.Add(new TelemetryRecord
                    {
                        Signal = "metric",
                        Kind = "Metric",
                        Source = source,
                        Time = time / 1_000_000d,
                        Name = Text(metric, "name") ?? "(unnamed metric)",
                        Scope = scope,
                        Value = Summarize(type, dataPoints),
                        Attributes = attributes,
                        Resource = resource,
                    });
                }
            }
        }

        return records;
    }

    /// <summary>
    /// Who sent a batch, from its resource: the browser script reports <c>telemetry.sdk.language = webjs</c>,
    /// and the .NET pipeline running in WebAssembly reports <c>os.type = browser</c>.
    /// </summary>
    public static string SourceOf(IReadOnlyDictionary<string, string> resource)
    {
        if (resource.TryGetValue("telemetry.sdk.language", out var language) && language == "webjs") return "browser";
        if (resource.TryGetValue("os.type", out var os) && os == "browser") return "wasm";
        return "server";
    }

    private static string? SessionOf(Dictionary<string, string> attributes, Dictionary<string, string> resource)
        => attributes.GetValueOrDefault("session.id") ?? resource.GetValueOrDefault("session.id");

    private static string NameOfLog(string? eventName, string? body, Dictionary<string, string> attributes)
    {
        if (string.IsNullOrEmpty(eventName) is false)
        {
            // A one-glance summary of the conventional events, so a page view reads as its path.
            var detail = eventName switch
            {
                "browser.page_view" => attributes.GetValueOrDefault("url.path") ?? attributes.GetValueOrDefault("page.title"),
                "browser.user_interaction" => attributes.GetValueOrDefault("target.name") ?? attributes.GetValueOrDefault("target.id") ?? attributes.GetValueOrDefault("target.element"),
                "browser.web_vital" => $"{attributes.GetValueOrDefault("web_vital.name")} {attributes.GetValueOrDefault("web_vital.value")} ({attributes.GetValueOrDefault("web_vital.rating")})",
                "exception" => $"{attributes.GetValueOrDefault("exception.type")}: {attributes.GetValueOrDefault("exception.message")}",
                _ => null,
            };

            return detail is null ? eventName : $"{eventName} {detail}";
        }

        if (attributes.TryGetValue("exception.type", out var exceptionType))
        {
            return $"{exceptionType}: {attributes.GetValueOrDefault("exception.message") ?? body}";
        }

        var text = body ?? "(empty log record)";
        return text.Length > 160 ? text[..157] + "..." : text;
    }

    private static string Summarize(string type, JsonElement[] points)
    {
        if (points.Length == 0) return "no points";

        switch (type)
        {
            case "gauge":
                return $"last={Format(Number(points[^1]))}";
            case "sum":
                return $"sum={Format(points.Sum(Number))}";
            case "histogram":
            case "exponentialHistogram":
                var count = points.Sum(p => Long(p, "count"));
                var sum = points.Sum(p => p.TryGetProperty("sum", out var s) ? NumberOf(s) : 0);
                return $"count={count} sum={Format(sum)}";
            default:
                return $"{points.Length} points";
        }
    }

    private static string PointValue(string type, JsonElement point) => type switch
    {
        "gauge" or "sum" => Format(Number(point)),
        "histogram" or "exponentialHistogram" => $"count={Long(point, "count")} sum={Format(point.TryGetProperty("sum", out var s) ? NumberOf(s) : 0)}",
        _ => "?",
    };

    private static double Number(JsonElement point)
        => point.TryGetProperty("asDouble", out var d) ? NumberOf(d)
         : point.TryGetProperty("asInt", out var i) ? NumberOf(i)
         : 0;

    private static double NumberOf(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => 0,
    };

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string? StatusOf(JsonElement status)
    {
        status.TryGetProperty("code", out var c);
        var number = c.ValueKind switch
        {
            JsonValueKind.Number => c.GetInt32(),
            JsonValueKind.String => c.GetString() switch { "STATUS_CODE_OK" => 1, "STATUS_CODE_ERROR" => 2, _ => 0 },
            _ => 0,
        };

        return number switch
        {
            1 => "Ok",
            2 => Text(status, "message") is { Length: > 0 } message ? $"Error: {message}" : "Error",
            _ => null,
        };
    }

    private static string? SpanKindOf(JsonElement span)
    {
        if (span.TryGetProperty("kind", out var kind) is false) return null;

        var number = kind.ValueKind switch
        {
            JsonValueKind.Number => kind.GetInt32(),
            JsonValueKind.String => System.Array.IndexOf(_spanKindNames, kind.GetString()),
            _ => 0,
        };

        return number is > 0 and < 6 ? _spanKinds[number] : null;
    }

    private static string SeverityName(int number) => number switch
    {
        <= 4 => "TRACE",
        <= 8 => "DEBUG",
        <= 12 => "INFO",
        <= 16 => "WARN",
        <= 20 => "ERROR",
        _ => "FATAL",
    };

    private static string? ScopeName(JsonElement scoped)
        => scoped.TryGetProperty("scope", out var scope) ? Text(scope, "name") : null;

    private static IEnumerable<JsonElement> Array(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return [];
        if (element.TryGetProperty(name, out var array) is false || array.ValueKind != JsonValueKind.Array) return [];
        return array.EnumerateArray();
    }

    private static string? Text(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || element.TryGetProperty(name, out var value) is false) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    /// <summary>
    /// A trace or span id. OTLP/JSON writes them as hex (not base64, unlike the generic protobuf JSON
    /// mapping); an empty string is "no id".
    /// </summary>
    private static string? Id(JsonElement element, string name)
        => Text(element, name) is { Length: > 0 } id ? id.ToLowerInvariant() : null;

    private static int Int(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || element.TryGetProperty(name, out var value) is false) return 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var i) => i,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0,
        };
    }

    private static long Long(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || element.TryGetProperty(name, out var value) is false) return 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var l) => l,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0,
        };
    }

    /// <summary>A fixed64 time: the JSON mapping writes 64-bit integers as strings, but numbers are accepted too.</summary>
    private static long Nanos(JsonElement element, string name) => Long(element, name);

    private static Dictionary<string, string> Attributes(JsonElement owner)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var attribute in Array(owner, "attributes"))
        {
            if (Text(attribute, "key") is not { Length: > 0 } key) continue;
            if (attribute.TryGetProperty("value", out var value) is false) continue;

            var text = AnyValue(value);
            if (text is null) continue;

            result[key] = text.Length > MaxValueLength ? text[..MaxValueLength] + "..." : text;
        }

        return result;
    }

    private static string? AnyValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return null;

        foreach (var property in value.EnumerateObject())
        {
            switch (property.Name)
            {
                case "stringValue":
                    return property.Value.GetString();
                case "boolValue":
                    return property.Value.ValueKind == JsonValueKind.True ? "true" : "false";
                case "intValue":
                    return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.GetRawText();
                case "doubleValue":
                    return Format(NumberOf(property.Value));
                case "bytesValue":
                    return property.Value.GetString();
                case "arrayValue":
                    var items = Array(property.Value, "values").Select(AnyValue);
                    return "[" + string.Join(", ", items) + "]";
                case "kvlistValue":
                    var builder = new StringBuilder("{");
                    foreach (var pair in Array(property.Value, "values"))
                    {
                        if (builder.Length > 1) builder.Append(", ");
                        builder.Append(Text(pair, "key")).Append(": ").Append(pair.TryGetProperty("value", out var v) ? AnyValue(v) : null);
                    }
                    return builder.Append('}').ToString();
            }
        }

        return null;
    }
}
