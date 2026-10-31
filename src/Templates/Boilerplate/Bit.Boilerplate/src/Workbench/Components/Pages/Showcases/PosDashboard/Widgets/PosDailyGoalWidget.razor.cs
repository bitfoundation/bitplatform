namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

public partial class PosDailyGoalWidget
{
    protected override Task<PosGoal> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetDailyGoal(filter, cancellationToken);

    protected override bool IsEmpty(PosGoal data) => data.Target == 0;
}
