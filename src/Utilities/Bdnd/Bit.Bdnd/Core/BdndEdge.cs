namespace Bit.Bdnd;

/// <summary>
/// An edge of a drop target: the one the pointer is closest to (<see cref="BdndDropState{TItem}.ClosestEdge"/>), which
/// tells a drop before an item from a drop after it.
/// </summary>
public enum BdndEdge
{
    /// <summary>The top edge: before the target in a vertical list.</summary>
    Top,

    /// <summary>The bottom edge: after the target in a vertical list.</summary>
    Bottom,

    /// <summary>The left edge: before the target in a horizontal list (left to right).</summary>
    Left,

    /// <summary>The right edge: after the target in a horizontal list (left to right).</summary>
    Right,
}
