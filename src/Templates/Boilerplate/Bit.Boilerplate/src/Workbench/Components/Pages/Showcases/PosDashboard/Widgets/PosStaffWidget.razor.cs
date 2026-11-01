namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>The team ranked by sales in the period, each with a bar relative to the top seller, who gets a trophy.</summary>
public partial class PosStaffWidget
{
    protected override Task<PosStaffMember[]> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetStaff(filter, cancellationToken);

    protected override bool IsEmpty(PosStaffMember[] data) => data.Length == 0;
}
