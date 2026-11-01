namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>The latest orders as a table: number, age, cashier, items, payment, total and status.</summary>
public partial class PosRecentOrdersWidget
{
    protected override Task<PosOrder[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetRecentOrders(filter, cancellationToken);

    protected override bool IsEmpty(PosOrder[] data) => data.Length == 0;

    /// <summary>How long ago an order was placed, in minutes and at least one.</summary>
    private string FormatAge(DateTimeOffset time)
    {
        var minutes = Math.Max(1, (int)(DateTimeOffset.Now - time).TotalMinutes);

        return Localizer[nameof(WorkbenchStrings.PosMinutesAgo), PosFormat.Number(minutes)];
    }
}
