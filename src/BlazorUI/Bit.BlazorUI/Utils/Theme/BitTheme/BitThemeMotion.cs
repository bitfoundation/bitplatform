namespace Bit.BlazorUI;

/// <summary>
/// The motion tokens of a theme. The durations and their factor, the spinner pair and the loop factor are written to their unreduced
/// <c>-full</c> tokens (<c>--bit-mot-duration-full</c>, ...), as a preset declares them, so a value set here is still
/// collapsed (or calmed) under <c>prefers-reduced-motion</c> and restored by <c>ForceAnimation</c>.
/// </summary>
public class BitThemeMotion
{
    /// <summary>The standard duration of a transition or a one-shot animation (<c>--bit-mot-duration-full</c>).</summary>
    public string? Duration { get; set; }

    /// <summary>The short duration, for small state changes (<c>--bit-mot-duration-short-full</c>).</summary>
    public string? DurationShort { get; set; }

    /// <summary>The long duration, for large surfaces entering and leaving (<c>--bit-mot-duration-long-full</c>).</summary>
    public string? DurationLong { get; set; }

    /// <summary>
    /// Multiplies every duration a page sets on a component itself - a public
    /// <c>--bit-&lt;Component&gt;-*-duration</c> variable or a duration parameter - which none of the durations
    /// above reach. It is a unitless number: 1 keeps the pace the page asked for, and reduced motion drops it
    /// to 0 so those durations collapse along with the theme's own (<c>--bit-mot-duration-factor-full</c>).
    /// </summary>
    public string? DurationFactor { get; set; }

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
