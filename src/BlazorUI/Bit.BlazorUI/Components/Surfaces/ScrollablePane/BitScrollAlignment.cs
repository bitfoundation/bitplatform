namespace Bit.BlazorUI;

/// <summary>
/// Where inside a scrolling viewport an element is left after it has been scrolled into view - the
/// <see cref="BitScrollablePane"/> that was asked to reveal it, or the BitVirtualize that was asked to
/// scroll to an index.
/// </summary>
public enum BitScrollAlignment
{
    /// <summary>
    /// The element is brought to the start of the viewport: its top edge to the top of the viewport, and its
    /// leading edge to the leading edge of the viewport.
    /// </summary>
    Start,

    /// <summary>
    /// The element is centered in the viewport along both axes.
    /// </summary>
    Center,

    /// <summary>
    /// The element is brought to the end of the viewport: its bottom edge to the bottom of the viewport, and
    /// its trailing edge to the trailing edge of the viewport.
    /// </summary>
    End,

    /// <summary>
    /// The viewport moves as little as it can: an element that is already fully in view is not moved to at
    /// all, and one that is not is brought to whichever edge it is nearest.
    /// </summary>
    Nearest
}
