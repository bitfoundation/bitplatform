namespace Bit.Belemetry;

/// <summary>
/// Settings of the Azure Monitor (Application Insights) exporter, which posts to the ingestion endpoint
/// (<c>/v2.1/track</c>) directly, so page views, requests, dependencies, exceptions and custom events land in their
/// usual Application Insights tables without a collector in between.
/// </summary>
public sealed class BelemetryAzureMonitorExporterOptions
{
    /// <summary>
    /// The Application Insights connection string
    /// (<c>InstrumentationKey=...;IngestionEndpoint=https://...;LiveEndpoint=https://...</c>).
    /// Defaults to <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> when set (outside the browser).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Supplies a Microsoft Entra ID access token (scope <c>https://monitor.azure.com//.default</c>) sent as
    /// <c>Authorization: Bearer</c>, for resources with local authentication disabled. Belemetry takes no
    /// dependency on Azure.Identity: pass e.g. <c>ct =&gt; credential.GetTokenAsync(...)</c>.
    /// </summary>
    /// <remarks>An exporter with a token provider is never handed to the browser script.</remarks>
    public Func<CancellationToken, ValueTask<string?>>? AccessTokenProvider { get; set; }

    /// <summary>
    /// Whether to send <c>ai.location.ip = 0.0.0.0</c> so the ingestion service does not derive the user's location
    /// from the client IP address. Defaults to <see langword="true"/>.
    /// </summary>
    public bool MaskIp { get; set; } = true;

    /// <summary>The request-body compression. Defaults to none.</summary>
    public BelemetryCompression Compression { get; set; } = BelemetryCompression.None;

    /// <summary>The maximum time one export request may take. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to stream Live Metrics (requests, dependencies, exceptions, CPU and memory, once per second while
    /// someone watches the Live Metrics blade). Not available in the browser. Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableLiveMetrics { get; set; }

    /// <summary>Whether the browser script exports to Application Insights too (with <see cref="BelemetryBrowserTransport.Direct"/>). Defaults to <see langword="true"/>.</summary>
    public bool IncludeInBrowser { get; set; } = true;

    /// <summary>The signals sent to Application Insights. Defaults to all three.</summary>
    public BelemetrySignals Signals { get; set; } = BelemetrySignals.All;
}
