namespace Bit.Belemetry;

/// <summary>
/// What the pipeline keeps for one page that relays to it (one per <see cref="BelemetryJsBridge"/>, so one per circuit
/// on Blazor Server): the rate limit a server puts on it, and the resource of its last batch, reused while it stays the
/// same so the export groups that page's records into one request instead of one per relayed batch.
/// </summary>
internal sealed class BelemetryRelaySource(BelemetryRateLimiter? limiter)
{
    /// <summary>The per-page budget of relayed records; null when the page is trusted (WebAssembly, Hybrid).</summary>
    public BelemetryRateLimiter? Limiter { get; } = limiter;

    /// <summary>The relayed resource attributes (raw JSON) <see cref="Resource"/> was built from.</summary>
    public string? ResourceKey { get; set; }

    public BelemetryResource? Resource { get; set; }

    /// <summary>The records dropped by <see cref="Limiter"/> so far.</summary>
    public long Dropped { get; set; }
}
