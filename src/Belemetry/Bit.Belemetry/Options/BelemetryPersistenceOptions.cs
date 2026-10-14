namespace Bit.Belemetry;

/// <summary>
/// Offline persistence: export requests that fail with a network error or a retryable status are kept on the
/// device and retried later, as long as the user granted <see cref="BelemetryFeatures.Persistence"/>.
/// </summary>
public sealed class BelemetryPersistenceOptions
{
    /// <summary>Whether persistence is used at all. Defaults to <see langword="true"/> (still subject to consent).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The folder the .NET side stores requests in (Hybrid and Server). Defaults to
    /// <c>{LocalApplicationData}/bit-belemetry/{service.name}</c>. In WebAssembly the browser storage is used.
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>The maximum size of the .NET store; the oldest requests are evicted past it. Defaults to 20 MB.</summary>
    public long MaxBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>The maximum size of the browser store (<c>localStorage</c> is small). Defaults to 1 MB.</summary>
    public long BrowserMaxBytes { get; set; } = 1024 * 1024;

    /// <summary>How long a stored request is kept before it is discarded unsent. Defaults to 48 hours.</summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(48);

    /// <summary>How often stored requests are retried. Defaults to 1 minute.</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMinutes(1);
}
