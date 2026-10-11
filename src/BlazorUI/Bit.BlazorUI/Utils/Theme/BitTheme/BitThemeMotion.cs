namespace Bit.BlazorUI;

public class BitThemeMotion
{
    public string? Duration { get; set; }
    public string? DurationShort { get; set; }
    public string? DurationLong { get; set; }

    /// <summary>
    /// Multiplies every duration a page sets on a component itself - a public
    /// <c>--bit-&lt;Component&gt;-*-duration</c> variable or a duration parameter - which none of the durations
    /// above reach. It is a unitless number: 1 keeps the pace the page asked for, and reduced motion drops it
    /// to 0 so those durations collapse along with the theme's own (<c>--bit-mot-duration-factor</c>).
    /// <br />
    /// Like the durations above, it is written to the effective token, and a theme applied inline outranks the
    /// reduced-motion query on <c>:root</c>: a value set here keeps every duration a page sets running for a reader
    /// who asked for reduced motion. Leave it unset unless the app answers that preference itself, and scale the
    /// page's durations with <c>--bit-mot-duration-factor-full</c> in CSS instead, which feeds both states.
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
