namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a polar area chart.
/// </summary>
public class BitChartLegacyPolarAreaConfig : BitChartLegacyConfigBase<BitChartLegacyPolarAreaOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPolarAreaConfig"/>.
    /// </summary>
    public BitChartLegacyPolarAreaConfig() : base(BitChartLegacyChartType.PolarArea) { }
}
