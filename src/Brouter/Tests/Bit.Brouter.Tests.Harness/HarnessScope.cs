namespace Bit.Brouter.Tests.Harness;

/// <summary>
/// Per-DI-scope state the harness exposes to the browser suites. One instance lives per prerender
/// request, per Server circuit, per WebAssembly app and per BlazorWebView page load, so comparing
/// the id a loader stamped into its result with the id of the scope rendering it tells a test which
/// of those produced the data on screen.
/// </summary>
public sealed class HarnessScope
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..12];

    public int DataLoaderRuns { get; set; }

    public int IntentTargetRuns { get; private set; }

    public int ViewportTargetRuns { get; private set; }

    /// <summary>Read by the /leave route's LeaveGuard; toggled from the page.</summary>
    public bool BlockLeave { get; set; }

    /// <summary>Raised when a preload loader ran, so the page showing the counters can re-render.</summary>
    public event Action? Changed;

    /// <summary>"Push /history/4", in the order Brouter reported its navigations to OnNavigating.</summary>
    public List<string> NavigationLog { get; } = [];

    public void RecordNavigation(string entry)
    {
        NavigationLog.Add(entry);
        Changed?.Invoke();
    }

    public int CountIntentTargetRun()
    {
        IntentTargetRuns++;
        Changed?.Invoke();
        return IntentTargetRuns;
    }

    public int CountViewportTargetRun()
    {
        ViewportTargetRuns++;
        Changed?.Invoke();
        return ViewportTargetRuns;
    }
}
