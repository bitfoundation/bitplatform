namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a bar chart.
/// </summary>
public class BitChartBarConfig : BitChartConfigBase<BitChartBarOptions>
{
    /// <summary>
    /// Creates a new instance of the <see cref="BitChartBarConfig"/> class.
    /// </summary>
    /// <param name="horizontal">
    /// If <see langword="true"/>, the chart-type will be set to <see cref="BitChartChartType.HorizontalBar"/>
    /// instead of <see cref="BitChartChartType.Bar"/> which turns this chart into a horizontal
    /// bar chart. If set to <see langword="true"/>, you also have to pass in <see langword="true"/> for
    /// the <see cref="BitChartBarDataset{T}"/>s.
    /// </param>
    public BitChartBarConfig(bool horizontal = false) : base(horizontal ? BitChartChartType.HorizontalBar : BitChartChartType.Bar) { }
}
