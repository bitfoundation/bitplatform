using System.Text;
using Bit.BevTools.Demo.Client.Models;
using Bit.BevTools.Demo.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Demo.Client.Extension;

/// <summary>
/// Everything an extension can contribute, written the way a library would write it: a panel, a state adapter, a
/// diagnostic rule, palette commands and a value formatter. Both hosts register it (Program.cs of the server and of the
/// client), so it works whichever runtime an Interactive Auto page gets. The Extending page walks through it.
/// </summary>
public sealed class DemoBevToolsExtension : IBevToolsExtension
{
    public string Name => "Demo store";

    public void Configure(IBevToolsExtensionBuilder builder) => builder
        .AddPanel(new BevToolsPanelDescriptor(DemoStorePanel.Id, "Store", typeof(DemoStorePanel)))
        .AddStateProvider(services => new FilterStateAdapter(services.GetRequiredService<FilterState>()))
        .AddDiagnosticRule(new SlowSyncRule())
        .AddCommand(new ShowStoreCommand())
        .AddCommand(new CopyCartCommand())
        // Wherever a Product is shown - a row's parameters, the cart's lines - it reads as one line, not a record to expand.
        .AddValueFormatter(typeof(Product), value => value is Product p ? $"{p.Name} · {p.Price:C}" : null);
}

/// <summary>
/// Shows <see cref="FilterState"/> - a plain store that knows nothing about BevTools - in the State panel. Its event
/// carries no information, so the adapter names each change by comparing with the values it saw last.
/// </summary>
internal sealed class FilterStateAdapter : IBevStateProvider
{
    private readonly FilterState _filter;
    private FilterSnapshot _last;

    public FilterStateAdapter(FilterState filter)
    {
        _filter = filter;
        _last = Snapshot();
        // Both live as long as the scope (a circuit, or the WebAssembly app), so the subscription needs no removal.
        _filter.Changed += OnChanged;
    }

    public string Name => "FilterState";

    public string? Description => "a store without BevTools support, shown by the demo's extension";

    public event Action<BevStateChangeInfo>? Changed;

    public object? GetSnapshot() => Snapshot();

    private FilterSnapshot Snapshot() => new(_filter.Query, _filter.Category, _filter.MaxPrice, _filter.Notifications);

    private void OnChanged()
    {
        var now = Snapshot();
        var change = now.Query != _last.Query ? new BevStateChangeInfo("SetQuery", now.Query)
            : now.Category != _last.Category ? new BevStateChangeInfo("SetCategory", now.Category)
            : now.MaxPrice != _last.MaxPrice ? new BevStateChangeInfo("SetMaxPrice", now.MaxPrice.ToString("C0"))
            : new BevStateChangeInfo("Notify", "nothing changed");
        _last = now;
        Changed?.Invoke(change);
    }

    private sealed record FilterSnapshot(string Query, string Category, decimal MaxPrice, int Notifications);
}

/// <summary>
/// Turns the custom "sync" events the Extending page records through <see cref="IBevToolsTimeline"/> into a finding.
/// A rule reads only what the context gives it - recent events and component snapshots - and changes nothing.
/// </summary>
internal sealed class SlowSyncRule : IBevDiagnosticRule
{
    private const double SlowMs = 1000;

    public string Id => "demo.slow-sync";

    public string Title => "Slow inventory sync";

    public string Description => "An inventory sync took longer than a second (contributed by the demo's extension).";

    public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
    {
        var slow = context.EventsInWindow(BevToolsEventKind.Custom, TimeSpan.FromMinutes(1))
            .Where(e => e.Category == "sync" && e.DurationMs > SlowMs)
            .ToList();
        if (slow.Count == 0)
        {
            yield break;
        }

        yield return new BevDiagnostic(
            Id,
            BevDiagnosticSeverity.Warning,
            slow.Count == 1 ? $"An inventory sync took {slow[0].DurationMs:0} ms" : $"{slow.Count} inventory syncs took over {SlowMs:0} ms",
            $"Longest: {slow.Max(e => e.DurationMs):0} ms, over the {SlowMs:0} ms this app allows. Recorded by the app through IBevToolsTimeline.BeginActivity.",
            "Sync in the background and show the last result meanwhile, or sync only what changed.",
            // The oldest slow sync still in the window: one finding that updates, not a new one per sync.
            Fingerprint: Id + ":" + slow[0].Id,
            RelatedEventIds: slow.Select(e => e.Id).ToList());
    }
}

internal sealed class ShowStoreCommand : IBevToolsCommand
{
    public string Id => "demo.store";

    public string Title => "Show the demo store";

    public string Category => "Demo store";

    public IReadOnlyList<string> Keywords => ["filter", "subscribers", "cart"];

    public Task ExecuteAsync(IBevToolsCommandContext context, string? argument)
    {
        context.ShowPanel(DemoStorePanel.Id);
        return Task.CompletedTask;
    }
}

/// <summary>Hands text to the browser through the command context, the way the built-in report and export do.</summary>
internal sealed class CopyCartCommand : IBevToolsCommand
{
    public string Id => "demo.copy-cart";

    public string Title => "Copy the cart as Markdown";

    public string Category => "Demo store";

    public IReadOnlyList<string> Keywords => ["clipboard", "share", "table"];

    public async Task ExecuteAsync(IBevToolsCommandContext context, string? argument)
    {
        var cart = context.Services.GetRequiredService<CartState>();
        var markdown = new StringBuilder("| Product | Quantity | Total |\n|---|---:|---:|\n");
        foreach (var line in cart.Lines)
        {
            markdown.Append($"| {line.Product.Name} | {line.Quantity} | {line.LineTotal:C} |\n");
        }

        markdown.Append($"| **Total** | {cart.ItemCount} | {cart.Total:C} |\n");
        context.Notify(await context.CopyTextAsync(markdown.ToString())
            ? $"Copied the cart ({cart.ItemCount} items) as Markdown."
            : "The clipboard is not available here.");
    }
}
