namespace Bit.Belemetry;

/// <summary>An exporter that ships batches as HTTP requests through the shared <see cref="BelemetryHttpSender"/>.</summary>
internal interface IBelemetryHttpExporter
{
    /// <summary>The name stored requests refer to, so a replay finds the exporter's current headers.</summary>
    string Name { get; }

    /// <summary>Serializes a batch into one request per signal it sends.</summary>
    IEnumerable<BelemetryHttpRequest> CreateRequests(BelemetryBatch batch);

    /// <summary>The headers every request carries; applied at send time, never persisted.</summary>
    IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Last-minute changes to the message, e.g. a bearer token.</summary>
    ValueTask PrepareAsync(HttpRequestMessage message, CancellationToken cancellationToken);

    /// <summary>The items of a partial-success response worth retrying, as a new request; null when none are.</summary>
    BelemetryHttpRequest? GetRetryForPartialSuccess(BelemetryHttpRequest request, int statusCode, string? responseBody);

    /// <summary>The maximum time one request may take.</summary>
    TimeSpan Timeout { get; }

    /// <summary>The least severe log record this exporter receives; null sends every record.</summary>
    BelemetrySeverity? MinimumSeverity { get; }
}
