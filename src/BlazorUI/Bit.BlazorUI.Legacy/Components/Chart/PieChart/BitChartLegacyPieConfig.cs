namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a pie chart.
/// </summary>
public class BitChartLegacyPieConfig : BitChartLegacyConfigBase<BitChartLegacyPieOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPieConfig"/>.
    /// </summary>
    /// <param name="useDoughnutType">
    /// If <see langword="true"/>, the chart-type will be set to <see cref="BitChartLegacyChartType.Doughnut"/>.
    /// If <see langword="false"/>, the chart-type will be set to <see cref="BitChartLegacyChartType.Pie"/>.
    /// This parameter can generally be left on <see langword="false"/> and only needs to be
    /// adjusted when you register a plugin which only works for doughnut charts or something similar.
    /// </param>
    public BitChartLegacyPieConfig(bool useDoughnutType = false) : base(useDoughnutType ? BitChartLegacyChartType.Doughnut : BitChartLegacyChartType.Pie) { }
}
