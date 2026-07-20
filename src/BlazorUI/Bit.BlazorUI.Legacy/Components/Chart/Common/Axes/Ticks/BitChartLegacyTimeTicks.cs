namespace Bit.BlazorUI.Legacy;

/// <summary>
/// The ticks-subconfig of a <see cref="BitChartLegacyTimeAxis"/>.
/// </summary>
public class BitChartLegacyTimeTicks : BitChartLegacyCartesianTicks
{
    /// <summary>
    /// Gets or sets how ticks are generated.
    /// </summary>
    public BitChartLegacyTickSource? Source { get; set; }
}
