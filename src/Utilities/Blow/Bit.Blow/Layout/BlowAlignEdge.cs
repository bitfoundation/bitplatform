namespace Bit.Blow;

/// <summary>Which edge (or axis) a set of nodes is lined up on.</summary>
public enum BlowAlignEdge
{
    /// <summary>Left edges line up with the leftmost node.</summary>
    Left,

    /// <summary>Horizontal centres line up with the centre of the group's bounding box.</summary>
    CenterX,

    /// <summary>Right edges line up with the rightmost node.</summary>
    Right,

    /// <summary>Top edges line up with the topmost node.</summary>
    Top,

    /// <summary>Vertical centres line up with the centre of the group's bounding box.</summary>
    CenterY,

    /// <summary>Bottom edges line up with the bottommost node.</summary>
    Bottom,
}
