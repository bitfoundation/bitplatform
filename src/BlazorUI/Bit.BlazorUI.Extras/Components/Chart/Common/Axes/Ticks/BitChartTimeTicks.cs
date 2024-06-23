namespace Bit.BlazorUI;

/// <summary>
/// The ticks-subconfig of a <see cref="BitChartTimeAxis"/>.
/// </summary>
public class BitChartTimeTicks : BitChartCartesianTicks
{
    /// <summary>
    /// Gets or sets how ticks are generated.
    /// </summary>
    public BitChartTickSource Source { get; set; }
}
