using System.Net;
using System.Text.Json.Nodes;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

public partial class DevMcpTelemetryReader
{
    public static readonly string[] Severities = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    private const int MaxValueLength = 2000;
    private const int MaxExceptionLength = 8000;

    private static readonly string[] OmittedLogAttributes = ["aspire.log_id", "{OriginalFormat}"];

    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private ServerApiSettings settings = default!;

    public bool Configured => settings.AspireDashboard?.Url is not null;

    public async Task<JsonNode?> Get(string pathAndQuery, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(pathAndQuery, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
            return null;

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("The Aspire dashboard refused the key in AspireDashboard:ApiKey, which has to match its Dashboard:Api:PrimaryApiKey.");

        response.EnsureSuccessStatusCode();

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonNode.ParseAsync(content, cancellationToken: cancellationToken);
    }

    public async Task<string[]> GetServiceNames(CancellationToken cancellationToken)
    {
        var resources = await Get("api/telemetry/resources", cancellationToken);

        return [.. (resources?.AsArray() ?? [])
            .Select(resource => resource?["name"]?.ToString())
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    public static List<TelemetryLog> ReadLogs(JsonNode? data)
    {
        List<TelemetryLog> logs = [];

        foreach (var resourceLogs in Items(data?["resourceLogs"]))
        {
            var resource = ServiceName(resourceLogs);

            foreach (var scopeLogs in Items(resourceLogs?["scopeLogs"]))
            {
                var category = scopeLogs?["scope"]?["name"]?.ToString();

                foreach (var record in Items(scopeLogs?["logRecords"]))
                {
                    var attributes = Attributes(record?["attributes"]);
                    var exception = attributes.GetValueOrDefault("exception.stacktrace")
                        ?? (attributes.GetValueOrDefault("exception.type") is { } type ? $"{type}: {attributes.GetValueOrDefault("exception.message")}" : null);

                    foreach (var key in attributes.Keys.Where(key => key.StartsWith("exception.", StringComparison.Ordinal) || OmittedLogAttributes.Contains(key)).ToArray())
                        attributes.Remove(key);

                    logs.Add(new(Timestamp(record?["timeUnixNano"]),
                        record?["severityText"]?.ToString() ?? Severity(record?["severityNumber"]),
                        resource,
                        category,
                        Cut(Text(record?["body"]), MaxValueLength),
                        Id(record?["traceId"]),
                        Id(record?["spanId"]),
                        attributes.Count is 0 ? null : attributes,
                        Cut(exception, MaxExceptionLength)));
                }
            }
        }

        return logs;
    }

    public static List<TelemetrySpan> ReadSpans(JsonNode? data)
    {
        List<TelemetrySpan> spans = [];

        foreach (var resourceSpans in Items(data?["resourceSpans"]))
        {
            var resource = ServiceName(resourceSpans);

            foreach (var scopeSpans in Items(resourceSpans?["scopeSpans"]))
            {
                var scope = scopeSpans?["scope"]?["name"]?.ToString();

                foreach (var span in Items(scopeSpans?["spans"]))
                {
                    var start = Timestamp(span?["startTimeUnixNano"]);
                    var end = Timestamp(span?["endTimeUnixNano"]);
                    var attributes = Attributes(span?["attributes"]);

                    spans.Add(new(Id(span?["traceId"]) ?? "",
                        Id(span?["spanId"]),
                        Id(span?["parentSpanId"]),
                        span?["name"]?.ToString(),
                        Kind(span?["kind"]),
                        resource,
                        scope,
                        start,
                        end,
                        start is null || end is null ? null : (end.Value - start.Value).TotalMilliseconds,
                        Status(span?["status"]?["code"]),
                        Cut(span?["status"]?["message"]?.ToString(), MaxValueLength),
                        attributes.Count is 0 ? null : attributes,
                        [.. Items(span?["events"]).Select(spanEvent => new TelemetrySpanEvent(spanEvent?["name"]?.ToString(),
                            Timestamp(spanEvent?["timeUnixNano"]),
                            Attributes(spanEvent?["attributes"]) is { Count: > 0 } eventAttributes ? eventAttributes : null))]));
                }
            }
        }

        return spans;
    }

    public static IEnumerable<object> SummarizeTraces(IEnumerable<TelemetrySpan> spans)
    {
        return spans.GroupBy(span => span.TraceId)
            .Select(trace =>
            {
                var spanIds = trace.Select(span => span.SpanId).ToHashSet();
                var root = trace.Where(span => span.ParentSpanId is null || spanIds.Contains(span.ParentSpanId) is false).MinBy(span => span.Start);
                var start = trace.Min(span => span.Start);
                var end = trace.Max(span => span.End);

                return new
                {
                    TraceId = trace.Key,
                    root?.Name,
                    Services = trace.Select(span => span.Resource).OfType<string>().Distinct().ToArray(),
                    Start = start,
                    DurationMs = start is null || end is null ? (double?)null : (end.Value - start.Value).TotalMilliseconds,
                    SpanCount = trace.Count(),
                    HasError = trace.Any(span => span.Status is "Error"),
                    FailedSpans = trace.Where(span => span.Status is "Error")
                        .Select(span => string.IsNullOrEmpty(span.StatusMessage) ? $"{span.Resource}: {span.Name}" : $"{span.Resource}: {span.Name} ({span.StatusMessage})")
                        .Take(5)
                        .ToArray() is { Length: > 0 } failed ? failed : null
                };
            })
            .OrderByDescending(trace => trace.Start);
    }

    public static (T[] Items, bool Truncated) Fit<T>(IEnumerable<T> items, int maxBytes)
    {
        List<T> fitted = [];
        var bytes = 0;

        foreach (var item in items)
        {
            bytes += Encoding.UTF8.GetByteCount(DevMcpJson.Serialize(item!));
            if (bytes > maxBytes && fitted.Count > 0)
                return ([.. fitted], true);
            fitted.Add(item);
        }

        return ([.. fitted], false);
    }

    private static IEnumerable<JsonNode?> Items(JsonNode? node) => node as JsonArray ?? [];

    private static string? ServiceName(JsonNode? resourceTelemetry)
    {
        return Items(resourceTelemetry?["resource"]?["attributes"])
            .FirstOrDefault(attribute => attribute?["key"]?.ToString() is "service.name") is { } serviceName ? Text(serviceName["value"]) : null;
    }

    private static Dictionary<string, string?> Attributes(JsonNode? attributes)
    {
        Dictionary<string, string?> values = [];

        foreach (var attribute in Items(attributes))
        {
            if (attribute?["key"]?.ToString() is { } key)
                values[key] = Cut(Text(attribute["value"]), key is "exception.stacktrace" ? MaxExceptionLength : MaxValueLength);
        }

        return values;
    }

    private static string? Text(JsonNode? value)
    {
        if (value is not JsonObject { Count: > 0 } anyValue)
            return null;

        var (_, inner) = anyValue.First();
        return inner is JsonValue ? inner.ToString() : inner?.ToJsonString();
    }

    private static string? Id(JsonNode? id) => id?.ToString() is { Length: > 0 } text ? text : null;

    private static DateTimeOffset? Timestamp(JsonNode? unixNanoseconds)
    {
        return long.TryParse(unixNanoseconds?.ToString(), out var nanoseconds) && nanoseconds > 0
            ? DateTimeOffset.UnixEpoch.AddTicks(nanoseconds / 100)
            : null;
    }

    private static string? Severity(JsonNode? severityNumber)
    {
        return int.TryParse(severityNumber?.ToString(), out var number) && number is >= 1 and <= 24
            ? Severities[(number - 1) / 4]
            : null;
    }

    private static string? Kind(JsonNode? kind) => kind?.ToString() switch
    {
        "1" => "Internal",
        "2" => "Server",
        "3" => "Client",
        "4" => "Producer",
        "5" => "Consumer",
        _ => null
    };

    private static string Status(JsonNode? code) => code?.ToString() switch
    {
        "1" => "Ok",
        "2" => "Error",
        _ => "Unset"
    };

    private static string? Cut(string? text, int maxLength)
    {
        return text is null || text.Length <= maxLength ? text : $"{text[..maxLength]}... ({text.Length - maxLength} more characters)";
    }

    public sealed record TelemetryLog(DateTimeOffset? Timestamp, string? Severity, string? Resource, string? Category, string? Message,
        string? TraceId, string? SpanId, Dictionary<string, string?>? Attributes, string? Exception);

    public sealed record TelemetrySpan(string TraceId, string? SpanId, string? ParentSpanId, string? Name, string? Kind, string? Resource,
        string? Scope, DateTimeOffset? Start, DateTimeOffset? End, double? DurationMs, string Status, string? StatusMessage,
        Dictionary<string, string?>? Attributes, TelemetrySpanEvent[] Events);

    public sealed record TelemetrySpanEvent(string? Name, DateTimeOffset? Timestamp, Dictionary<string, string?>? Attributes);
}
