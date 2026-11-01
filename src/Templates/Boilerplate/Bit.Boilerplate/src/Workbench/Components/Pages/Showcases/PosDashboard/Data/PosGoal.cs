namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>The day's sales target and what has been sold so far.</summary>
public sealed record PosGoal(decimal Target, decimal Achieved)
{
    /// <summary>How much of the target is reached, from 0 to 1; 0 without a target.</summary>
    public double Progress => Target == 0 ? 0 : (double)Math.Min(1, Achieved / Target);
}
