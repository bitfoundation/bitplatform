namespace Bit.Belemetry;

/// <summary>
/// The server side (Blazor Server, static SSR, the server half of Auto). By default Belemetry only serves the browser
/// script there - an ASP.NET Core host usually has its own OpenTelemetry setup - and its .NET pipeline stays off.
/// </summary>
public sealed class BelemetryServerOptions
{
    /// <summary>
    /// Whether the .NET pipeline (ILogger, Activity, Meter collection and export) also runs on the server.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// The server pipeline is process-wide: it serves every user at once, so it is governed by
    /// <see cref="Features"/> - operator telemetry - and not by any one user's consent. Per-user consent keeps
    /// governing everything the browser script collects, and the <see cref="IBelemetry"/> calls of a circuit, which
    /// are routed through that user's browser.
    /// </remarks>
    public bool CollectDotnetTelemetry { get; set; }

    /// <summary>
    /// The features the server pipeline collects when <see cref="CollectDotnetTelemetry"/> is on. Defaults to errors,
    /// logs, traces and metrics.
    /// </summary>
    public BelemetryFeatures Features { get; set; } = BelemetryFeatures.Errors | BelemetryFeatures.Logs | BelemetryFeatures.Traces | BelemetryFeatures.Metrics;
}
