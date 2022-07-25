namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a line chart.
/// </summary>
public class BitChartLineConfig : BitChartConfigBase<BitChartLineOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLineConfig"/>.
    /// </summary>
    public BitChartLineConfig() : base(BitChartChartType.Line) { }
}
