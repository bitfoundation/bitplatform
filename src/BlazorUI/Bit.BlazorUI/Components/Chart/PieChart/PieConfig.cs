namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a pie chart.
/// </summary>
public class PieConfig : ConfigBase<PieOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="PieConfig"/>.
    /// </summary>
    /// <param name="useDoughnutType">
    /// If <see langword="true"/>, the chart-type will be set to <see cref="ChartType.Doughnut"/>.
    /// If <see langword="false"/>, the chart-type will be set to <see cref="ChartType.Pie"/>.
    /// This parameter can generally be left on <see langword="false"/> and only needs to be
    /// adjusted when you register a plugin which only works for doughnut charts or something similar.
    /// </param>
    public PieConfig(bool useDoughnutType = false) : base(useDoughnutType ? ChartType.Doughnut : ChartType.Pie) { }
}
