namespace Bit.BlazorUI;

public class BitThemeMotion
{
    public string? Duration { get; set; }
    public string? DurationShort { get; set; }
    public string? DurationLong { get; set; }
    public string? EasingStandard { get; set; }

    /// <summary>The entry curve of popups, callouts and sheets (<c>--bit-mot-easing-decelerate</c>).</summary>
    public string? EasingDecelerate { get; set; }

    /// <summary>The exit curve of popups, callouts and sheets (<c>--bit-mot-easing-accelerate</c>).</summary>
    public string? EasingAccelerate { get; set; }

    /// <summary>
    /// The duration of every circular spinner in the library. Looping animations cannot use the
    /// durations above, which collapse to a near-zero value under reduced motion and would render as
    /// flicker, so they have their own that slows down instead.
    /// </summary>
    public string? DurationSpinner { get; set; }

    /// <summary>
    /// The timing function of every circular spinner in the library.
    /// </summary>
    public string? EasingSpinner { get; set; }

    /// <summary>
    /// Multiplies the duration of a looping animation that is not a spinner (an indeterminate bar, a
    /// staggered loader). It is a unitless number: 1 leaves the animations at their designed speed,
    /// and reduced motion raises it so they stretch rather than stop.
    /// </summary>
    public string? LoopFactor { get; set; }
}
