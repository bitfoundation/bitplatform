namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a polar area chart.
/// </summary>
public class BitChartPolarAreaConfig : BitChartConfigBase<BitChartPolarAreaOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartPolarAreaConfig"/>.
    /// </summary>
    public BitChartPolarAreaConfig() : base(BitChartChartType.PolarArea) { }
}
