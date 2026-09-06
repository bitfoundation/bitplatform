namespace Bit.BlazorUI;

/// <summary>
/// Where along a side a surface anchored to that side is aligned, which is the second half of a placement:
/// <see cref="BitSide"/> says which side of the anchor the surface is on, this says where along that side it
/// lines up.
/// </summary>
/// <remarks>
/// The axis it aligns on is the one the side leaves free - a surface above or below its anchor is aligned
/// horizontally, one beside it vertically. Start, Center and End are logical and are what most placements
/// want: on the horizontal axis Start is the left in an LTR context and the right in an RTL one, and on the
/// vertical axis Start is the top in either. Left and Right are physical and stay on the same side of the
/// screen in both reading directions, the way <see cref="BitSide"/>'s own pair does, for a placement that must
/// not flip.
/// <br />
/// Not every parameter typed as an alignment honours every value: the physical pair is only meaningful where
/// the free axis is the horizontal one, and only where a component resolves the placement itself rather than
/// leaving it to a positioning engine. Each parameter names the values it accepts, and falls back to its own
/// default for the rest.
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
    End,

    /// <summary>
    /// Lined up with the left edge, in both reading directions. It names a side of the screen, so it is only
    /// meaningful for a surface above or below its anchor; one beside its anchor is aligned on the vertical
    /// axis, which has no left, and is centered along it instead.
    /// </summary>
    Left,

    /// <summary>
    /// Lined up with the right edge, in both reading directions. It names a side of the screen, so it is only
    /// meaningful for a surface above or below its anchor; one beside its anchor is aligned on the vertical
    /// axis, which has no right, and is centered along it instead.
    /// </summary>
    Right
}
