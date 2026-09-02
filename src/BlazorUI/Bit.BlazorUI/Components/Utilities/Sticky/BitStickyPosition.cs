namespace Bit.BlazorUI;

/// <summary>
/// The edges of the scrolling container a BitSticky pins itself to.
/// </summary>
public enum BitStickyPosition
{
    /// <summary>
    /// Sticks to the top edge while the container scrolls vertically.
    /// </summary>
    Top,

    /// <summary>
    /// Sticks to the bottom edge while the container scrolls vertically.
    /// </summary>
    Bottom,

    /// <summary>
    /// Sticks to whichever vertical edge the scroll carries it to: the top while scrolling down past
    /// it, the bottom while it is still below the fold.
    /// </summary>
    TopAndBottom,

    /// <summary>
    /// Sticks to the start edge while the container scrolls horizontally - the left edge in LTR, the
    /// right edge in RTL.
    /// </summary>
    Start,

    /// <summary>
    /// Sticks to the end edge while the container scrolls horizontally - the right edge in LTR, the
    /// left edge in RTL.
    /// </summary>
    End,

    /// <summary>
    /// Sticks to whichever horizontal edge the scroll carries it to, following the reading direction
    /// the way Start and End do.
    /// </summary>
    StartAndEnd,
}
