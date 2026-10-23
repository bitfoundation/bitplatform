namespace Bit.BevTools;

/// <summary>An error captured from any source (render, event handler, JS, HTTP, circuit, logger).</summary>
public sealed class BevErrorRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required string Source { get; init; }

    public required string ExceptionType { get; init; }

    public required string Message { get; init; }

    /// <summary><see cref="Message"/> with its values replaced (<see cref="BevErrorGroups.Template"/>): "Order {n} not found".</summary>
    public string MessageTemplate { get; init; } = "";

    /// <summary>The key of the errors that are one problem: source, type, message template, where it was thrown.</summary>
    public string Group { get; init; } = "";

    public string? StackTrace { get; init; }

    public string? SourceLocation { get; init; }

    public long? ComponentInstanceId { get; init; }

    public string? ComponentName { get; init; }

    /// <summary>The UI event or activity that immediately preceded the error, when known.</summary>
    public long? PrecedingEventId { get; init; }

    public long TimelineEventId { get; set; }

    /// <summary>Stable key for grouping repeats.</summary>
    public required string Fingerprint { get; init; }

    public int Count { get; internal set; } = 1;

    public DateTimeOffset LastSeen { get; internal set; }

    /// <summary>
    /// "Type: message" of the innermost exception when it differs from the outer one. The exception object itself is
    /// deliberately not retained: its graph can reference components, services and closures, and the error center
    /// keeps hundreds of records for the lifetime of the session.
    /// </summary>
    public string? InnerError { get; init; }
}

public sealed class BevHttpRecord
{
    internal int StoreGeneration { get; set; }

    internal bool FailureCounted { get; set; }

    public required long Id { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required string Method { get; init; }

    public required string Url { get; init; }

    public int? StatusCode { get; set; }

    public string? ReasonPhrase { get; set; }

    public double? DurationMs { get; set; }

    public long? RequestBytes { get; set; }

    public long? ResponseBytes { get; set; }

    public string? Error { get; set; }

    public string? ContentType { get; set; }

    public IReadOnlyList<KeyValuePair<string, string>>? RequestHeaders { get; set; }

    public IReadOnlyList<KeyValuePair<string, string>>? ResponseHeaders { get; set; }

    public long? TriggerEventId { get; init; }

    public string? TriggerDescription { get; init; }

    /// <summary>The component method that sent the request ("OrderRow.OnInitializedAsync"), when the stack shows one.</summary>
    public string? CallerComponent { get; init; }

    /// <summary>The application method the component sent it through ("ProductApi.GetOrderDetailsAsync"), when there is one.</summary>
    public string? CallerVia { get; init; }

    /// <summary>"OrderRow.OnInitializedAsync via ProductApi.GetOrderDetailsAsync", or null when no component is on the stack.</summary>
    public string? Caller => CallerComponent is null ? null : CallerVia is null ? CallerComponent : CallerComponent + " via " + CallerVia;

    /// <summary>Why no sender was found, when the stack was looked at and none was; null when it was not looked at.</summary>
    public string? CallerMissing { get; init; }

    public long TimelineEventId { get; set; }

    /// <summary>
    /// The request's cancellation token was cancelled before it completed. Inside the handler pipeline the caller
    /// cancelling and <c>HttpClient.Timeout</c> elapsing look the same (the client tells them apart above the handlers),
    /// so a cancelled request is neither called a failure nor a timeout.
    /// </summary>
    public bool IsCancelled { get; set; }

    public bool IsFailed => !IsCancelled && (Error is not null || StatusCode >= 400);

    public bool IsPending => DurationMs is null;

    /// <summary>Method + the (redacted) URL, query values included: what "identical requests" compares.</summary>
    public string Signature => Method + " " + Url;

    /// <summary>Method + path + the names of the query parameters, without their values: "GET /api/products?q".</summary>
    public string Endpoint => _endpoint ??= BevHttpEndpoints.Of(Method, Url);

    /// <summary>
    /// <see cref="Endpoint"/> with id-like path segments as <c>{id}</c>: "GET /api/orders/{id}". What the Network panel
    /// groups by, and what a request per list item has in common.
    /// </summary>
    public string EndpointTemplate => _endpointTemplate ??= BevHttpEndpoints.Template(Method, Url);

    // Method and Url never change: the rules and the panel ask for these on every evaluation and refresh.
    private string? _endpoint;
    private string? _endpointTemplate;
}

/// <summary>Requests to the same endpoint, whatever their query values: a request per keystroke is one endpoint.</summary>
public static class BevHttpEndpoints
{
    /// <summary>"GET https://host/api/products?q": the path as it is, the query by parameter names.</summary>
    public static string Of(string method, string url)
    {
        var hash = url.IndexOf('#');
        if (hash >= 0)
        {
            url = url[..hash];
        }

        var query = url.IndexOf('?');
        if (query < 0)
        {
            return method + " " + url;
        }

        var keys = url[(query + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.IndexOf('=') is var eq and >= 0 ? p[..eq] : p)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        return method + " " + url[..query] + "?" + string.Join("&", keys);
    }

    /// <summary>
    /// <see cref="Of"/> with the path segments that are identifiers replaced by <c>{id}</c>: numbers, GUIDs and long
    /// hexadecimal strings (hashes, object ids). <c>/api/orders/1001</c> and <c>/api/orders/1002</c> are one endpoint.
    /// Words are kept, so <c>/api/orders/recent</c> is another.
    /// </summary>
    public static string Template(string method, string url)
    {
        var endpoint = Of(method, url);
        var start = method.Length + 1;
        var scheme = endpoint.IndexOf("://", start, StringComparison.Ordinal);
        var pathStart = scheme < 0 ? start : endpoint.IndexOf('/', scheme + 3);
        if (pathStart < 0)
        {
            return endpoint;
        }

        var query = endpoint.IndexOf('?', pathStart);
        var pathEnd = query < 0 ? endpoint.Length : query;
        var segments = endpoint[pathStart..pathEnd].Split('/');
        var changed = false;
        for (var i = 0; i < segments.Length; i++)
        {
            if (IsIdentifier(segments[i]))
            {
                segments[i] = "{id}";
                changed = true;
            }
        }

        return changed ? endpoint[..pathStart] + string.Join('/', segments) + endpoint[pathEnd..] : endpoint;
    }

    private static bool IsIdentifier(string segment)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        if (segment.All(char.IsAsciiDigit) || Guid.TryParse(segment, out _))
        {
            return true;
        }

        return segment.Length >= 16 && segment.All(char.IsAsciiHexDigit);
    }

    /// <summary>Requests grouped by <see cref="BevHttpRecord.EndpointTemplate"/>, with their counts and durations.</summary>
    internal static List<BevHttpEndpointSummary> Summarize(IEnumerable<BevHttpRecord> requests) =>
        requests.GroupBy(r => r.EndpointTemplate, StringComparer.Ordinal).Select(g =>
        {
            var durations = g.Where(r => !r.IsPending && !r.IsCancelled && r.DurationMs is not null).Select(r => r.DurationMs!.Value).Order().ToArray();
            var last = g.MaxBy(r => r.StartedAt)!;
            return new BevHttpEndpointSummary(
                g.Key,
                g.Count(),
                g.Count(r => r.IsFailed),
                g.Count(r => r.IsCancelled),
                g.Count(r => r.IsPending),
                durations.Length == 0 ? null : BevStats.Percentile(durations, 50),
                durations.Length == 0 ? null : BevStats.Percentile(durations, 95),
                durations.Length == 0 ? null : durations[^1],
                // A total only when every response reported its size (a chunked body has no Content-Length): a sum
                // of some of them would read as the whole. Requests that got no response received nothing.
                g.Any(r => r.StatusCode is not null && !r.IsCancelled) && g.Where(r => r.StatusCode is not null && !r.IsCancelled).All(r => r.ResponseBytes is not null)
                    ? g.Sum(r => r.ResponseBytes ?? 0)
                    : null,
                last.StartedAt,
                last.IsCancelled ? null : last.StatusCode,
                g.Select(r => r.Url).Distinct(StringComparer.Ordinal).Count())
            {
                Callers = g.Where(r => r.CallerComponent is not null).GroupBy(r => r.CallerComponent!, StringComparer.Ordinal)
                    .OrderByDescending(c => c.Count()).Take(5).Select(c => (c.Key, c.Count())).ToList(),
                CallerCount = g.Where(r => r.CallerComponent is not null).Select(r => r.CallerComponent).Distinct(StringComparer.Ordinal).Count(),
            };
        }).ToList();
}

/// <summary>
/// One endpoint's requests in the buffer. Durations are of the requests that completed (failed ones included, cancelled
/// and pending ones not); sizes are response bodies as the transport reported them, null unless every response did.
/// </summary>
internal sealed record BevHttpEndpointSummary(
    string Endpoint,
    int Requests,
    int Failed,
    int Cancelled,
    int Pending,
    double? P50Ms,
    double? P95Ms,
    double? MaxMs,
    long? ResponseBytes,
    DateTimeOffset LastAt,
    int? LastStatus,
    int DistinctUrls)
{
    /// <summary>The component methods that sent its requests, most first, with how many each: the first five.</summary>
    public IReadOnlyList<(string Component, int Requests)> Callers { get; init; } = [];

    /// <summary>How many different component methods sent its requests, all of them.</summary>
    public int CallerCount { get; init; }
}

public enum BevJsInteropDirection
{
    DotNetToJs,
    JsToDotNet,
}

public sealed class BevJsInteropRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required BevJsInteropDirection Direction { get; init; }

    public required string Identifier { get; init; }

    public double? DurationMs { get; set; }

    public bool Succeeded { get; set; } = true;

    public string? Error { get; set; }

    public int ArgumentCount { get; init; }

    public string? ArgumentTypes { get; init; }

    public string? ResultType { get; init; }

    public long? ComponentInstanceId { get; init; }

    public string? ComponentName { get; init; }

    public bool IsModuleCall { get; init; }

    public bool IsSync { get; init; }

    public long TimelineEventId { get; set; }

    /// <summary>High-resolution start timestamp, so the duration never depends on the timeline still holding the event.</summary>
    internal long StartTicks { get; init; }
}

/// <summary>One recorded change of a registered state provider.</summary>
public sealed class BevStateChangeRecord
{
    public required long Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required string Provider { get; init; }

    public string? Action { get; init; }

    public string? Detail { get; init; }

    /// <summary>Path → (before, after) for members whose display value changed.</summary>
    public required IReadOnlyList<BevStateDiffEntry> Changes { get; init; }

    /// <summary>Flattened snapshot after the change (bounded), used by history browsing.</summary>
    public required IReadOnlyDictionary<string, string> Snapshot { get; init; }

    public long? TriggerEventId { get; init; }

    public long TimelineEventId { get; set; }
}

public sealed record BevStateDiffEntry(string Path, string? Before, string? After)
{
    public string Kind => Before is null ? "added" : After is null ? "removed" : "changed";
}
