namespace Bit.Bdnd;

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
