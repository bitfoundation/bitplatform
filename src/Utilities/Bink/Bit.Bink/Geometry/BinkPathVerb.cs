namespace Bit.Bink;

/// <summary>Path drawing verbs. The number of coordinates consumed by each verb is given by <see cref="BinkPathGeometry.CoordinateCount"/>.</summary>
public enum BinkPathVerb : byte
{
    /// <summary>Starts a new sub-path (2 coordinates).</summary>
    MoveTo = 0,
    /// <summary>Straight line (2 coordinates).</summary>
    LineTo = 1,
    /// <summary>Quadratic Bézier: control point then end point (4 coordinates).</summary>
    QuadTo = 2,
    /// <summary>Cubic Bézier: two control points then end point (6 coordinates).</summary>
    CubicTo = 3,
    /// <summary>Closes the current sub-path (0 coordinates).</summary>
    Close = 4,
}
