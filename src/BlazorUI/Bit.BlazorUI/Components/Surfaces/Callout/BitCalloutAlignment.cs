namespace Bit.BlazorUI;

/// <summary>
/// How a callout is lined up with its anchor along the axis it is not placed on.
/// </summary>
/// <remarks>
/// The alignment runs across the placement: a callout above or below the anchor is aligned horizontally,
/// and one beside the anchor is aligned vertically. It is applied before the callout is kept within the
/// screen, so a callout that would hang off an edge is still slid back onto it.
/// </remarks>
public enum BitCalloutAlignment
{
    /// <summary>
    /// Lined up with the edge the anchor starts at - its left edge in a left-to-right layout for a callout
    /// above or below it, and its top edge for a callout beside it. This is the default.
    /// </summary>
    Start,

    /// <summary>
    /// Centered on the anchor.
    /// </summary>
    Center,

    /// <summary>
    /// Lined up with the edge the anchor ends at - its right edge in a left-to-right layout for a callout
    /// above or below it, and its bottom edge for a callout beside it.
    /// </summary>
    End
}
