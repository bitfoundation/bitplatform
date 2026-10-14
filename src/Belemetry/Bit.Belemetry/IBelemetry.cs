namespace Bit.Belemetry;

/// <summary>
/// Manual telemetry from C#: custom events, page views and handled exceptions, the authenticated user, and flushing.
/// Everything else (logs, traces, metrics, browser instrumentation) is collected automatically.
/// </summary>
/// <remarks>
/// On WebAssembly and Hybrid these calls go through the .NET pipeline. On Blazor Server they are routed through the
/// circuit's browser, so that user's consent applies (and they need an interactive render mode); with the server
/// pipeline enabled (<see cref="BelemetryServerOptions.CollectDotnetTelemetry"/>), calls made while no browser is
/// connected - e.g. during prerendering - go through the server pipeline instead.
/// </remarks>
public interface IBelemetry
{
    /// <summary>Records a custom event (feature <see cref="BelemetryFeatures.Events"/>).</summary>
    void TrackEvent(string name, IReadOnlyDictionary<string, object?>? attributes = null);

    /// <summary>Records a page view (feature <see cref="BelemetryFeatures.PageViews"/>). Navigation is already tracked automatically; use this for virtual pages such as dialogs or wizard steps.</summary>
    void TrackPageView(string? name = null, string? url = null, IReadOnlyDictionary<string, object?>? attributes = null);

    /// <summary>Records a handled exception (feature <see cref="BelemetryFeatures.Errors"/>).</summary>
    void TrackException(Exception exception, IReadOnlyDictionary<string, object?>? attributes = null);

    /// <summary>
    /// Sets (or with <see langword="null"/> clears) the authenticated user id sent as <c>user.id</c>. Only kept while
    /// <see cref="BelemetryFeatures.UserTracking"/> is granted.
    /// </summary>
    ValueTask SetUserAsync(string? userId);

    /// <summary>Exports everything queued now - e.g. before a Hybrid app is suspended.</summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
