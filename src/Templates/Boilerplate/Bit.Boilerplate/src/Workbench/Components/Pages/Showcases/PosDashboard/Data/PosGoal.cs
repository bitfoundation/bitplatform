namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

public sealed record PosGoal(decimal Target, decimal Achieved)
{
    public double Progress => Target == 0 ? 0 : (double)Math.Min(1, Achieved / Target);
}
