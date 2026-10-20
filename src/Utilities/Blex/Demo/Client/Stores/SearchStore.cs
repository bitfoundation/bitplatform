namespace Bit.Blex.Demo.Client.Stores;

/// <summary>
/// A realistic type-ahead: <c>Latest</c> concurrency means each keystroke supersedes the
/// in-flight request, and the trailing <see cref="CancellationToken"/> makes the generator emit
/// <c>CancelSearch()</c>. The <c>SearchIsLoading</c> / <c>SearchError</c> properties come for free.
/// </summary>
[BlexStoreAttribute(Name = "search")]
public partial class SearchStore
{
    private static readonly string[] Catalog =
    [
        "Blazor WebAssembly", "Blazor Server", "Blazor Hybrid", "Razor Components",
        "Source Generators", "Roslyn Analyzers", "Incremental Generators",
        "Redux DevTools", "Time-travel Debugging", "Entity Adapter",
        "State Management", "Dependency Injection", "Middleware Pipeline",
    ];

    [BlexStateAttribute] private string _query = "";
    [BlexStateAttribute] private IReadOnlyList<string> _results = [];
    [BlexStateAttribute] private bool _simulateFailure;

    [BlexComputedAttribute] private bool ComputeHasResults() => Results.Count > 0;

    [BlexActionAttribute] private void OnSetSimulateFailure(bool value) => SimulateFailure = value;

    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnSearch(string query, CancellationToken ct)
    {
        Query = query;

        // A deliberately slow "network" call so supersession and cancellation are observable.
        await Task.Delay(700, ct);

        if (SimulateFailure)
            throw new InvalidOperationException($"The search service is unavailable (simulated) for '{query}'.");

        Results = string.IsNullOrWhiteSpace(query)
            ? []
            : [.. Catalog.Where(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))];
    }

    [BlexActionAttribute]
    private void OnClear()
    {
        Query = "";
        Results = [];
        Keystrokes = 0;
        RequestsStarted = 0;
    }

    // ---- Debounced variant -------------------------------------------------------------------
    // Same search, gated behind a quiet period. Latest still matters: once a request does get
    // through, a later one that also gets through supersedes it.

    [BlexStateAttribute] private int _keystrokes;
    [BlexStateAttribute] private int _requestsStarted;

    [BlexActionAttribute] private void OnCountKeystroke() => Keystrokes++;

    [BlexEffectAttribute(DebounceMilliseconds = 300, Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnTypeAhead(string query, CancellationToken ct)
    {
        // Reached only by the invocation that survived the 300 ms gate, so this counter is the
        // honest count of requests the service would have seen.
        RequestsStarted++;
        Query = query;

        await Task.Delay(700, ct);

        if (SimulateFailure)
            throw new InvalidOperationException($"The search service is unavailable (simulated) for '{query}'.");

        Results = string.IsNullOrWhiteSpace(query)
            ? []
            : [.. Catalog.Where(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))];
    }
}
