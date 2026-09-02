namespace Bit.Butil;

/// <summary>
/// A cross-document view transition reaching one of its two hook points - the payload of
/// <see cref="ViewTransition.OnPageSwap"/> and <see cref="ViewTransition.OnPageReveal"/>.
/// </summary>
public class CrossDocumentTransitionEvent
{
    /// <summary>Which hook fired: <c>"pageswap"</c> on the outgoing document, <c>"pagereveal"</c> on the incoming one.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True when a view transition is actually in play. False means the navigation is happening
    /// without one - a cross-origin navigation, a document that didn't opt in, or a user who prefers
    /// reduced motion.
    /// </summary>
    public bool HasTransition { get; set; }

    /// <summary>The URL being navigated away from. Empty when the browser doesn't report it.</summary>
    public string FromUrl { get; set; } = string.Empty;

    /// <summary>The URL being navigated to.</summary>
    public string ToUrl { get; set; } = string.Empty;

    /// <summary>
    /// How the navigation happened: <c>"push"</c>, <c>"replace"</c>, <c>"reload"</c> or
    /// <c>"traverse"</c>. This is what tells a back navigation from a forward one, so the animation
    /// can go the right way.
    /// </summary>
    public string NavigationType { get; set; } = string.Empty;
}
