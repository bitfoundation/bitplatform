namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard.Data;

/// <summary>A KPI's value, the previous period's value and the trend its sparkline draws.</summary>
public sealed record PosKpi(decimal Value, decimal Previous, decimal[] Trend)
{
    /// <summary>The change from the previous period as a fraction; 0 when there was nothing before.</summary>
    public decimal Change => Previous == 0 ? 0 : (Value - Previous) / Previous;
}
