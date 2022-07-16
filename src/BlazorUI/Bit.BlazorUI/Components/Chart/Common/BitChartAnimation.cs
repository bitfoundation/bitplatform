namespace Bit.BlazorUI;

/// <summary>
/// The animation-subconfig of <see cref="BitChartBaseConfigOptions"/>.
/// Specifies options for the animations in this chart.
/// </summary>
public class BitChartAnimation
{
    /// <summary>
    /// Gets or sets the number of milliseconds an animation takes.
    /// </summary>
    public long? Duration { get; set; }

    /// <summary>
    /// Gets or sets the easing function to use.
    /// See <a href="https://easings.net"/> for reference.
    /// </summary>
    public BitChartEasing Easing { get; set; }

    // TODO OnProgress Callback called on each step of an animation.
    // TODO OnComplete Callback called at the end of an animation.
}
