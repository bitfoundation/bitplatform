namespace Bit.BlazorUI;

/// <summary>
/// Represents the config for a radar chart.
/// </summary>
public class BitChartRadarConfig : BitChartConfigBase<BitChartRadarOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartRadarConfig"/>.
    /// </summary>
    public BitChartRadarConfig() : base(BitChartChartType.Radar) { }
}
