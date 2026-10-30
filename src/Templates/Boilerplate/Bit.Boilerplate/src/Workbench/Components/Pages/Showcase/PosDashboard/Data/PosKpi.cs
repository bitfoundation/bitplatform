namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard.Data;

public sealed record PosKpi(decimal Value, decimal Previous, decimal[] Trend)
{
    public decimal Change => Previous == 0 ? 0 : (Value - Previous) / Previous;
}
