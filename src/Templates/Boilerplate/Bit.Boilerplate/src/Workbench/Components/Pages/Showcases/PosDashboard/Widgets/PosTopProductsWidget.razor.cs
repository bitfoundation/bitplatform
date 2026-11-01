namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>The best-selling products ranked by revenue, each with a bar relative to the first.</summary>
public partial class PosTopProductsWidget
{
    protected override Task<PosTopProduct[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetTopProducts(filter, cancellationToken);

    protected override bool IsEmpty(PosTopProduct[] data) => data.Length == 0;
}
