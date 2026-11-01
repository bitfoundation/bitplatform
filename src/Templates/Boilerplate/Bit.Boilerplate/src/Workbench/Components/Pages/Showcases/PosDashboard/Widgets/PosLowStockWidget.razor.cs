namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>Products running low, each with a Reorder button that simulates placing an order.</summary>
public partial class PosLowStockWidget
{
    // Products whose reorder is being sent; those already reordered are kept in the store.
    private readonly HashSet<string> ordering = [];

    [AutoInject] private PosDashboardStore store = default!;

    protected override Task<PosStockAlert[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetLowStock(filter, cancellationToken);

    protected override bool IsEmpty(PosStockAlert[] data) => data.Length == 0;

    /// <summary>Simulates sending a reorder, then marks the product as ordered.</summary>
    private async Task Reorder(PosStockAlert item)
    {
        ordering.Add(item.NameKey);

        await ScenarioState.Save();

        ordering.Remove(item.NameKey);
        store.Reorder(item.NameKey);
    }
}
