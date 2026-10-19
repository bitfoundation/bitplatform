using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

namespace Bit.Belemetry.Tests.Harness.Web.Collector;

/// <summary>
/// The capturing receiver's store: every OTLP/HTTP and Azure Monitor ingestion request any harness host exported,
/// decoded into flat records. One per host process; a test picks its own records out by <c>session.id</c> or
/// <c>harness.marker</c> rather than clearing it, because the suites share a host between test classes.
/// </summary>
public sealed class HarnessCollector
{
    private readonly object _lock = new();
    private readonly CollectedTelemetry _all = new();

    /// <summary>Decodes and keeps one exported request. Returns the number of items it carried.</summary>
    public int Add(string signal, string path, string? contentType, string? contentEncoding, string? userAgent, byte[] body)
    {
        if (string.Equals(contentEncoding, "gzip", StringComparison.OrdinalIgnoreCase)) body = Gunzip(body);

        var request = new CollectedRequest
        {
            Signal = signal,
            Path = path,
            ContentType = contentType,
            ContentEncoding = contentEncoding,
            Length = body.Length,
            Time = DateTimeOffset.UtcNow,
            UserAgent = userAgent,
        };

        var decoded = new CollectedTelemetry();
        var items = 0;

        // Protobuf bodies are kept as a request only: the harness exports OTLP/JSON, and the serializers themselves are
        // covered byte for byte by the unit tests.
        if (contentType is null || contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                items = signal switch
                {
                    "traces" => DecodeTraces(document.RootElement, decoded.Spans),
                    "logs" => DecodeLogs(document.RootElement, decoded.Logs),
                    "metrics" => DecodeMetrics(document.RootElement, decoded.Metrics),
                    "azure" => DecodeEnvelopes(document.RootElement, decoded.Envelopes),
                    _ => 0,
                };
                request.Decoded = true;
            }
            catch (JsonException)
            {
                request.Decoded = false;
            }
        }

        lock (_lock)
        {
            _all.Requests.Add(request);
            _all.Spans.AddRange(decoded.Spans);
            _all.Logs.AddRange(decoded.Logs);
            _all.Metrics.AddRange(decoded.Metrics);
            _all.Envelopes.AddRange(decoded.Envelopes);
        }

        return items;
    }

    public void AddEcho(CollectedEcho echo)
    {
        lock (_lock) _all.Echoes.Add(echo);
    }

    /// <summary>
    /// A copy of what was received. With <paramref name="session"/> only the records of that browser session
    /// (<c>session.id</c> / <c>ai.session.id</c>); with <paramref name="marker"/> only those carrying that
    /// <c>harness.marker</c>. Requests and echoes are never filtered.
    /// </summary>
    public CollectedTelemetry Snapshot(string? session = null, string? marker = null)
    {
        bool Matches(Dictionary<string, string?> attributes, string sessionKey) =>
            (session is null || (attributes.TryGetValue(sessionKey, out var s) && s == session)) &&
            (marker is null || (attributes.TryGetValue(HarnessTelemetry.MarkerAttribute, out var m) && m == marker));

        lock (_lock)
        {
            return new CollectedTelemetry
            {
                Requests = [.. _all.Requests],
                Echoes = [.. _all.Echoes],
                Spans = [.. _all.Spans.Where(s => Matches(s.Attributes, "session.id"))],
                Logs = [.. _all.Logs.Where(l => Matches(l.Attributes, "session.id"))],
                Metrics = [.. _all.Metrics.Where(m => marker is null && session is null || m.Points.Any(p => Matches(p, "session.id")))],
                Envelopes = [.. _all.Envelopes.Where(e => (session is null || (e.Tags.TryGetValue("ai.session.id", out var s) && s == session)) &&
                                                          (marker is null || (e.Properties.TryGetValue(HarnessTelemetry.MarkerAttribute, out var m) && m == marker)))],
            };
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _all.Requests.Clear();
            _all.Spans.Clear();
            _all.Logs.Clear();
            _all.Metrics.Clear();
            _all.Envelopes.Clear();
            _all.Echoes.Clear();
        }
    }

    private static int DecodeTraces(JsonElement root, List<CollectedSpan> spans)
    {
        var count = 0;
        foreach (var resourceSpans in Array(root, "resourceSpans"))
        {
            var resource = ResourceOf(resourceSpans);
            foreach (var scopeSpans in Array(resourceSpans, "scopeSpans"))
            {
                var scope = ScopeOf(scopeSpans);
                foreach (var span in Array(scopeSpans, "spans"))
                {
                    spans.Add(new CollectedSpan
                    {
                        TraceId = String(span, "traceId") ?? string.Empty,
                        SpanId = String(span, "spanId") ?? string.Empty,
                        ParentSpanId = String(span, "parentSpanId") is { Length: > 0 } parent ? parent : null,
                        Name = String(span, "name") ?? string.Empty,
                        Kind = Int(span, "kind"),
                        StatusCode = span.TryGetProperty("status", out var status) ? Int(status, "code") : 0,
                        Scope = scope,
                        Attributes = Attributes(span),
                        Resource = resource,
                    });
                    count++;
                }
            }
        }
        return count;
    }

    private static int DecodeLogs(JsonElement root, List<CollectedLog> logs)
    {
        var count = 0;
        foreach (var resourceLogs in Array(root, "resourceLogs"))
        {
            var resource = ResourceOf(resourceLogs);
            foreach (var scopeLogs in Array(resourceLogs, "scopeLogs"))
            {
                var scope = ScopeOf(scopeLogs);
                foreach (var log in Array(scopeLogs, "logRecords"))
                {
                    logs.Add(new CollectedLog
                    {
                        SeverityNumber = Int(log, "severityNumber"),
                        SeverityText = String(log, "severityText"),
                        Body = log.TryGetProperty("body", out var body) ? ValueToString(body) : null,
                        EventName = String(log, "eventName"),
                        TraceId = String(log, "traceId") is { Length: > 0 } traceId ? traceId : null,
                        SpanId = String(log, "spanId") is { Length: > 0 } spanId ? spanId : null,
                        Scope = scope,
                        Attributes = Attributes(log),
                        Resource = resource,
                    });
                    count++;
                }
            }
        }
        return count;
    }

    private static int DecodeMetrics(JsonElement root, List<CollectedMetric> metrics)
    {
        var count = 0;
        foreach (var resourceMetrics in Array(root, "resourceMetrics"))
        {
            var resource = ResourceOf(resourceMetrics);
            foreach (var scopeMetrics in Array(resourceMetrics, "scopeMetrics"))
            {
                var scope = ScopeOf(scopeMetrics);
                foreach (var metric in Array(scopeMetrics, "metrics"))
                {
                    var collected = new CollectedMetric { Name = String(metric, "name") ?? string.Empty, Scope = scope, Resource = resource };
                    foreach (var type in new[] { "sum", "gauge", "histogram", "exponentialHistogram", "summary" })
                    {
                        if (metric.TryGetProperty(type, out var data) is false) continue;

                        collected.Type = type;
                        foreach (var point in Array(data, "dataPoints")) collected.Points.Add(Attributes(point));
                    }
                    metrics.Add(collected);
                    count++;
                }
            }
        }
        return count;
    }

    private static int DecodeEnvelopes(JsonElement root, List<CollectedEnvelope> envelopes)
    {
        // The ingestion endpoint takes a JSON array, or newline-delimited envelopes.
        IEnumerable<JsonElement> items = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : [root];

        var count = 0;
        foreach (var item in items)
        {
            var envelope = new CollectedEnvelope
            {
                Name = String(item, "name") ?? string.Empty,
                IKey = String(item, "iKey"),
                Time = String(item, "time"),
            };

            if (item.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Object)
            {
                foreach (var tag in tags.EnumerateObject()) envelope.Tags[tag.Name] = ValueToString(tag.Value);
            }

            if (item.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                envelope.BaseType = String(data, "baseType");
                if (data.TryGetProperty("baseData", out var baseData) && baseData.ValueKind == JsonValueKind.Object)
                {
                    envelope.BaseData = baseData.GetRawText();
                    envelope.DataName = String(baseData, "name") ?? String(baseData, "message");
                    if (baseData.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in properties.EnumerateObject()) envelope.Properties[property.Name] = ValueToString(property.Value);
                    }
                }
            }

            envelopes.Add(envelope);
            count++;
        }
        return count;
    }

    private static Dictionary<string, string?> ResourceOf(JsonElement container) =>
        container.TryGetProperty("resource", out var resource) ? Attributes(resource) : [];

    private static string? ScopeOf(JsonElement container) =>
        container.TryGetProperty("scope", out var scope) ? String(scope, "name") : null;

    private static Dictionary<string, string?> Attributes(JsonElement element)
    {
        var attributes = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var attribute in Array(element, "attributes"))
        {
            if (String(attribute, "key") is not { } key) continue;
            attributes[key] = attribute.TryGetProperty("value", out var value) ? AnyValueToString(value) : null;
        }
        return attributes;
    }

    /// <summary>An OTLP <c>AnyValue</c> as a string; arrays and maps as JSON.</summary>
    private static string? AnyValueToString(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return ValueToString(value);

        foreach (var property in value.EnumerateObject())
        {
            switch (property.Name)
            {
                case "stringValue":
                case "intValue":
                case "doubleValue":
                case "boolValue":
                case "bytesValue":
                    return ValueToString(property.Value);
                case "arrayValue":
                    var values = Array(property.Value, "values").Select(AnyValueToString);
                    return JsonSerializer.Serialize(values);
                case "kvlistValue":
                    return property.Value.GetRawText();
            }
        }
        return null;
    }

    /// <summary>A body (<c>{"stringValue": ...}</c>) or a plain JSON value, as a string.</summary>
    private static string? ValueToString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.Object => AnyValueToString(value),
        _ => value.GetRawText(),
    };

    private static IEnumerable<JsonElement> Array(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray()
            : [];

    private static string? String(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? ValueToString(value) : null;

    private static int Int(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || element.TryGetProperty(name, out var value) is false) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static byte[] Gunzip(byte[] body)
    {
        using var input = new MemoryStream(body);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
