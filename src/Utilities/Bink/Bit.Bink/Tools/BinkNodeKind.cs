namespace Bit.Bink;

/// <summary>What a node of an element edited by <see cref="BinkNodeEditTool"/> is.</summary>
public enum BinkNodeKind : byte
{
    /// <summary>A point the outline passes through: a polygon's corner, a line's end, a path's anchor.</summary>
    Anchor = 0,

    /// <summary>A Bézier control point of a path, which the curve bends toward without passing through.</summary>
    Control = 1,
}
