namespace Bit.Belemetry;

/// <summary>An exporter whose output is plain HTTP requests, which the shared sender delivers, retries and persists.</summary>
internal interface IBelemetryHttpExporter
{
    /// <summary>A short name for diagnostics, also stored with persisted requests to find their exporter again.</summary>
    string Name { get; }

    /// <summary>Serializes <paramref name="batch"/> into the requests that deliver it (none when there is nothing for this exporter).</summary>
    IEnumerable<BelemetryHttpRequest> CreateRequests(BelemetryBatch batch);

    /// <summary>
    /// The configured headers (e.g. an API key), read from the current options. Added to every request when it is sent,
    /// never stored with it.
    /// </summary>
    IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Adds what cannot be persisted with the request - e.g. a fresh access token - right before sending.</summary>
    ValueTask PrepareAsync(HttpRequestMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Inspects a success response that may be partial (OTLP <c>partial_success</c>, Azure 206) and returns a request
    /// with just the items worth retrying, or <see langword="null"/>.
    /// </summary>
    BelemetryHttpRequest? GetRetryForPartialSuccess(BelemetryHttpRequest request, int statusCode, string? responseBody);

    /// <summary>The maximum time one request may take.</summary>
    TimeSpan Timeout { get; }
}
