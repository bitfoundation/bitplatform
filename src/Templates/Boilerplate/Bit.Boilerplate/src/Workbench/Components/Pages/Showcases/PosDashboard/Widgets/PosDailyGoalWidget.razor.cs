namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Widgets;

/// <summary>How much of the day's sales target is reached, as a ring and in money; no target shows the empty state.</summary>
public partial class PosDailyGoalWidget
{
    protected override Task<PosGoal> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken) => DataService.GetDailyGoal(filter, cancellationToken);

    protected override bool IsEmpty(PosGoal data) => data.Target == 0;
}
