namespace Bit.Belemetry;

/// <summary>
/// Lets singletons reach the browser script, which is only reachable through a scoped <c>IJSRuntime</c>: the scoped
/// bridge registers its delegates here once it has started. Only meaningful in WebAssembly and Hybrid, where one
/// page/WebView belongs to the process.
/// </summary>
internal sealed class BelemetryBrowserBridgeRegistry
{
    /// <summary>Sends pre-serialized requests through the script (keepalive fetch, retry, persistence).</summary>
    public Func<IReadOnlyList<BelemetryHttpRequest>, ValueTask>? SendRequests { get; set; }

    /// <summary>Pushes a consent decision to the script.</summary>
    public Func<BelemetryFeatures, ValueTask>? SetConsent { get; set; }

    /// <summary>Pushes the authenticated user id to the script.</summary>
    public Func<string?, ValueTask>? SetUser { get; set; }
}
