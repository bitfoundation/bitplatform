namespace Bit.Blow;

/// <summary>
/// Which edges of a node a resize control manipulates. Corners combine two flags.
/// </summary>
[Flags]
public enum BlowResizeDirection
{
    /// <summary>No edge; the control does nothing.</summary>
    None = 0,

    /// <summary>The top edge. Dragging it keeps the bottom edge fixed.</summary>
    Top = 1,

    /// <summary>The right edge. Dragging it keeps the left edge fixed.</summary>
    Right = 2,

    /// <summary>The bottom edge. Dragging it keeps the top edge fixed.</summary>
    Bottom = 4,

    /// <summary>The left edge. Dragging it keeps the right edge fixed.</summary>
    Left = 8,

    /// <summary>The top-left corner.</summary>
    TopLeft = Top | Left,

    /// <summary>The top-right corner.</summary>
    TopRight = Top | Right,

    /// <summary>The bottom-left corner.</summary>
    BottomLeft = Bottom | Left,

    /// <summary>The bottom-right corner.</summary>
    BottomRight = Bottom | Right,
}
