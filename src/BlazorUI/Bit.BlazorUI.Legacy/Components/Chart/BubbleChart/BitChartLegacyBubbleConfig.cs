namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a bubble chart.
/// </summary>
public class BitChartLegacyBubbleConfig : BitChartLegacyConfigBase<BitChartLegacyLineOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyBubbleConfig"/>.
    /// </summary>
    public BitChartLegacyBubbleConfig() : base(BitChartLegacyChartType.Bubble) { }
}
