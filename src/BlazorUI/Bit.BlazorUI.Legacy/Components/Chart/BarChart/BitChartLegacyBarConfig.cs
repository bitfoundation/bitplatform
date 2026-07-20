namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a bar chart.
/// </summary>
public class BitChartLegacyBarConfig : BitChartLegacyConfigBase<BitChartLegacyBarOptions>
{
    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyBarConfig"/> class.
    /// </summary>
    /// <param name="horizontal">
    /// If <see langword="true"/>, the chart-type will be set to <see cref="BitChartLegacyChartType.HorizontalBar"/>
    /// instead of <see cref="BitChartLegacyChartType.Bar"/> which turns this chart into a horizontal
    /// bar chart. If set to <see langword="true"/>, you also have to pass in <see langword="true"/> for
    /// the <see cref="BitChartLegacyBarDataset{T}"/>s.
    /// </param>
    public BitChartLegacyBarConfig(bool horizontal = false) : base(horizontal ? BitChartLegacyChartType.HorizontalBar : BitChartLegacyChartType.Bar) { }
}
