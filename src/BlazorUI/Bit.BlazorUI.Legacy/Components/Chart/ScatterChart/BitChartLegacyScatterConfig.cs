namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the config for a scatter chart.
/// <para>
/// Scatter charts are based on basic line charts with the x axis changed to a
/// <see cref="BitChartLegacyLinearCartesianAxis"/> (unless otherwise specified).
/// Therefore, many configuration options are from the line chart.
/// </para>
/// </summary>
public class BitChartLegacyScatterConfig : BitChartLegacyConfigBase<BitChartLegacyLineOptions>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyScatterConfig"/>.
    /// </summary>
    public BitChartLegacyScatterConfig() : base(BitChartLegacyChartType.Scatter) { }
}
