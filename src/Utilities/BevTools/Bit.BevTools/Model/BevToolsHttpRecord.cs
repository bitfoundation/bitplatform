namespace Bit.BevTools;

public sealed class BevToolsHttpRecord
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
    /// The W3C trace id the request carried to the server in its <c>traceparent</c> header - what the server's spans and
    /// logs are filed under in a tracing backend (OpenTelemetry) - when it carried one. HttpClient adds the header below
    /// BevTools' handler, so it is read once the request completes; a client that propagates nothing (the browser's
    /// HttpClient by default) has none.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>The client span the server's work is a child of: the <c>traceparent</c> header's parent id.</summary>
    public string? SpanId { get; set; }

    /// <summary>What the request is beyond plain HTTP, from its content types and path: "gRPC", "server-sent events", "GraphQL".</summary>
    public string? Protocol { get; set; }

    /// <summary>The GraphQL operation its body names (<c>operationName</c>), when bodies are captured.</summary>
    public string? GraphQlOperation { get; set; }

    /// <summary>The request body, redacted and bounded, when <see cref="BevToolsOptions.CaptureHttpBodies"/> asked for it.</summary>
    public string? RequestBody { get; set; }

    /// <summary>Why the request body was not read, when bodies are captured and it was not.</summary>
    public string? RequestBodyNote { get; set; }

    public string? ResponseBody { get; set; }

    public string? ResponseBodyNote { get; set; }

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
    public string Endpoint => _endpoint ??= BevToolsHttpEndpoints.Of(Method, Url);

    /// <summary>
    /// <see cref="Endpoint"/> with id-like path segments as <c>{id}</c>: "GET /api/orders/{id}". What the Network panel
    /// groups by, and what a request per list item has in common.
    /// </summary>
    public string EndpointTemplate => _endpointTemplate ??= BevToolsHttpEndpoints.Template(Method, Url);

    // Method and Url never change: the rules and the panel ask for these on every evaluation and refresh.
    private string? _endpoint;
    private string? _endpointTemplate;
}
