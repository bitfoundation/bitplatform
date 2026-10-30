using Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Widgets;

public partial class PosStaffWidget
{
    protected override Task<PosStaffMember[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetStaff(filter, cancellationToken);

    protected override bool IsEmpty(PosStaffMember[] data) => data.Length == 0;

    private static string BarWidth(decimal value, decimal top) => FormattableString.Invariant($"{(top == 0 ? 0 : value / top * 100):0.#}%");
}
