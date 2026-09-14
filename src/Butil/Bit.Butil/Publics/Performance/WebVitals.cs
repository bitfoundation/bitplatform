namespace Bit.Butil;

/// <summary>
/// The three Core Web Vitals as they stand right now, already reduced from the entries they are
/// computed over: <see cref="Lcp"/> is the last candidate, <see cref="Cls"/> the worst session
/// window, and <see cref="Inp"/> the near-worst interaction.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance_API">https://developer.mozilla.org/en-US/docs/Web/API/Performance_API</see>
/// </summary>
/// <remarks>
/// A snapshot, not a verdict: all three only settle when the page is hidden or unloaded, and the
/// value read a second into a visit will not be the value that gets reported. Read it again on
/// <c>visibilitychange</c> if you are sending it anywhere.
/// <br/>
/// A metric with nothing to report is <c>null</c>, which is not the same as zero -
/// <see cref="Inp"/> is null until the user interacts, and <see cref="Lcp"/> is null on a browser
/// that does not implement it.
/// </remarks>
public class WebVitals
{
    /// <summary>Largest Contentful Paint, in milliseconds. Good below 2 500.</summary>
    public double? Lcp { get; set; }

    /// <summary>Cumulative Layout Shift - the worst session window, unitless. Good below 0.1.</summary>
    public double? Cls { get; set; }

    /// <summary>Interaction to Next Paint, in milliseconds. Good below 200.</summary>
    public double? Inp { get; set; }

    /// <summary>First Contentful Paint, in milliseconds - not a Core Web Vital, but the one that explains a bad <see cref="Lcp"/>.</summary>
    public double? Fcp { get; set; }

    /// <summary>Time to First Byte, in milliseconds - <see cref="Fcp"/>'s own floor.</summary>
    public double? Ttfb { get; set; }

    /// <summary>How many interactions <see cref="Inp"/> was chosen from. Below about 50, treat it as one slow interaction rather than as a score.</summary>
    public int InteractionCount { get; set; }

    /// <summary>How many layout shifts went into <see cref="Cls"/>.</summary>
    public int LayoutShiftCount { get; set; }
}
