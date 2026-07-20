namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a radar chart.
/// </summary>
public class BitChartLegacyRadarConfig : BitChartLegacyConfigBase<BitChartLegacyRadarOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyRadarConfig"/>.
    /// </summary>
    public BitChartLegacyRadarConfig() : base(BitChartLegacyChartType.Radar) { }
}
