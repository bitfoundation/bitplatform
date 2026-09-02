namespace Bit.Butil;

/// <summary>
/// Whether a programmatic scroll animates or jumps.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/scrollIntoView">Element.scrollIntoView()</see>
/// </summary>
public enum ScrollBehavior
{
    /// <summary>
    /// Follow the element's <c>scroll-behavior</c> CSS property, which is itself <c>auto</c>
    /// (an instant jump) unless a stylesheet says otherwise. The default.
    /// </summary>
    Auto,

    /// <summary>Jump straight to the destination, ignoring any CSS <c>scroll-behavior</c>.</summary>
    Instant,

    /// <summary>Animate, at the engine's own pace.</summary>
    Smooth
}
