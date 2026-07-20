namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a line chart.
/// </summary>
public class BitChartLegacyLineConfig : BitChartLegacyConfigBase<BitChartLegacyLineOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyLineConfig"/>.
    /// </summary>
    public BitChartLegacyLineConfig() : base(BitChartLegacyChartType.Line) { }
}
