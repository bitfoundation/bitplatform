namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

public partial class PosTopProductsWidget
{
    protected override Task<PosTopProduct[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetTopProducts(filter, cancellationToken);

    protected override bool IsEmpty(PosTopProduct[] data) => data.Length == 0;

    private static string BarWidth(decimal value, decimal top) => FormattableString.Invariant($"{(top == 0 ? 0 : value / top * 100):0.#}%");
}
