using Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Widgets;

public partial class PosRecentOrdersWidget
{
    protected override Task<PosOrder[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetRecentOrders(filter, cancellationToken);

    protected override bool IsEmpty(PosOrder[] data) => data.Length == 0;

    private string FormatAge(DateTimeOffset time)
    {
        var minutes = Math.Max(1, (int)(DateTimeOffset.Now - time).TotalMinutes);

        return Localizer[nameof(WorkbenchStrings.PosMinutesAgo), PosFormat.Number(minutes)];
    }
}
