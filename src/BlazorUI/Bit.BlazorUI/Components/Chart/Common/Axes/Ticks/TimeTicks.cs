namespace Bit.BlazorUI;

/// <summary>
/// The ticks-subconfig of a <see cref="TimeAxis"/>.
/// </summary>
public class TimeTicks : CartesianTicks
{
    /// <summary>
    /// Gets or sets how ticks are generated.
    /// </summary>
    public TickSource Source { get; set; }
}
