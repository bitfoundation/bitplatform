namespace Bit.Butil;

/// <summary>
/// How keen the browser should be to act on a speculation rule, matching the strings the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Speculation_Rules_API">Speculation Rules API</see>
/// accepts.
/// </summary>
/// <remarks>
/// The dial between wasted work and saved time. Prerendering a page the user never visits costs them
/// bandwidth, CPU and battery, and runs that page's scripts for nothing - so the eager end belongs to
/// a handful of URLs you are confident about, not to every link on the page.
/// </remarks>
public enum SpeculationEagerness
{
    /// <summary>As soon as the rule is seen. For the one destination you are sure of - the next step of a wizard.</summary>
    Immediate,

    /// <summary>Almost immediate; the browser may defer slightly under load.</summary>
    Eager,

    /// <summary>On hover, or after a short dwell. A good default for a link the user is looking at.</summary>
    Moderate,

    /// <summary>On pointer-down - just early enough to save the connection, never speculative.</summary>
    Conservative,
}
