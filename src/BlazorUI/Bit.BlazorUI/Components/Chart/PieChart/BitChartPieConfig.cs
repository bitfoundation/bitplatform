namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a pie chart.
/// </summary>
public class BitChartPieConfig : BitChartConfigBase<BitChartPieOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartPieConfig"/>.
    /// </summary>
    /// <param name="useDoughnutType">
    /// If <see langword="true"/>, the chart-type will be set to <see cref="BitChartChartType.Doughnut"/>.
    /// If <see langword="false"/>, the chart-type will be set to <see cref="BitChartChartType.Pie"/>.
    /// This parameter can generally be left on <see langword="false"/> and only needs to be
    /// adjusted when you register a plugin which only works for doughnut charts or something similar.
    /// </param>
    public BitChartPieConfig(bool useDoughnutType = false) : base(useDoughnutType ? BitChartChartType.Doughnut : BitChartChartType.Pie) { }
}
