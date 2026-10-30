using Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Widgets;

public partial class PosLowStockWidget
{
    private readonly HashSet<string> ordering = [];
    private readonly HashSet<string> ordered = [];

    protected override Task<PosStockAlert[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetLowStock(filter, cancellationToken);

    protected override bool IsEmpty(PosStockAlert[] data) => data.Length == 0;

    private async Task Reorder(PosStockAlert item)
    {
        ordering.Add(item.NameKey);

        await ScenarioState.Save(CurrentCancellationToken);

        ordering.Remove(item.NameKey);
        ordered.Add(item.NameKey);
    }
}
