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

/// <summary>The edges of a drop target a drag is placed against (<see cref="BdndDropTarget{TItem}.Edges"/>).</summary>
[Flags]
public enum BdndEdges
{
    /// <summary>No edges: the target is one place, not a position between two.</summary>
    None = 0,

    /// <summary>The top edge.</summary>
    Top = 1,

    /// <summary>The bottom edge.</summary>
    Bottom = 2,

    /// <summary>The left edge.</summary>
    Left = 4,

    /// <summary>The right edge.</summary>
    Right = 8,

    /// <summary>Top and bottom: an item of a vertical list.</summary>
    Vertical = Top | Bottom,

    /// <summary>Left and right: an item of a horizontal list.</summary>
    Horizontal = Left | Right,

    /// <summary>Every edge: a tile of a grid.</summary>
    All = Vertical | Horizontal,
}

/// <summary>The names the module and the <c>data-bit-bdnd-edge</c> attribute give the edges.</summary>
internal static class BdndEdgeNames
{
    public static BdndEdge? Parse(string? edge) => edge switch
    {
        "top" => BdndEdge.Top,
        "bottom" => BdndEdge.Bottom,
        "left" => BdndEdge.Left,
        "right" => BdndEdge.Right,
        _ => null,
    };

    public static string? Name(BdndEdge? edge) => edge switch
    {
        BdndEdge.Top => "top",
        BdndEdge.Bottom => "bottom",
        BdndEdge.Left => "left",
        BdndEdge.Right => "right",
        _ => null,
    };
}
