namespace Bit.BlazorUI;

/// <summary>
/// Determines the animation the <see cref="BitShimmer"/> plays while it stands in for content that has not arrived yet.
/// </summary>
public enum BitShimmerAnimation
{
    /// <summary>
    /// A highlight band sweeps across the placeholder from one side to the other, reversing with the direction of the page.
    /// </summary>
    Wave,

    /// <summary>
    /// The placeholder breathes between full and reduced opacity, which is cheaper to paint than the wave and calmer on a page full of placeholders.
    /// </summary>
    Pulse,

    /// <summary>
    /// The placeholder fades all the way out and back in, a heavier version of the pulse for a single placeholder that has to be noticed.
    /// </summary>
    Fade,

    /// <summary>
    /// No animation at all: the placeholder is a static block, which is what a page with a great many of them - or one that already
    /// carries a progress indicator of its own - is better off showing.
    /// </summary>
    None
}
