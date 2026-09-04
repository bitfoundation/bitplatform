namespace Bit.BlazorUI;

/// <summary>
/// Where along a side a surface anchored to that side is aligned, which is the second half of a placement:
/// <see cref="BitSide"/> says which side of the anchor the surface is on, this says where along that side it
/// lines up.
/// </summary>
/// <remarks>
/// The axis it aligns on is the one the side leaves free - a surface above or below its anchor is aligned
/// horizontally, one beside it vertically - and it is always logical: on the horizontal axis Start is the left
/// in an LTR context and the right in an RTL one, and on the vertical axis Start is the top in either.
/// </remarks>
public enum BitSideAlignment
{
    /// <summary>
    /// Lined up with the start of the side: the left edge in LTR (the right in RTL) for a surface above or
    /// below its anchor, the top edge for one beside it.
    /// </summary>
    Start,

    /// <summary>
    /// Centered along the side.
    /// </summary>
    Center,

    /// <summary>
    /// Lined up with the end of the side: the right edge in LTR (the left in RTL) for a surface above or
    /// below its anchor, the bottom edge for one beside it.
    /// </summary>
    End
}
