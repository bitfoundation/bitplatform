namespace Bit.Bink;

/// <summary>The shape a <see cref="BinkShapeTool"/> draws.</summary>
public enum BinkShapeKind : byte
{
    /// <summary>A rectangle.</summary>
    Rectangle = 0,
    /// <summary>An ellipse inscribed in the dragged rectangle.</summary>
    Ellipse = 1,
    /// <summary>A straight line.</summary>
    Line = 2,
    /// <summary>A line with an arrow head at the end.</summary>
    Arrow = 3,
    /// <summary>A translucent highlight rectangle.</summary>
    Highlight = 4,
    /// <summary>A diamond touching the middle of each side of the dragged rectangle: a flowchart's decision.</summary>
    Diamond = 5,
    /// <summary>A triangle pointing up, filling the dragged rectangle.</summary>
    Triangle = 6,
    /// <summary>A regular polygon of <see cref="BinkShapeTool.PolygonSides"/> sides, filling the dragged rectangle.</summary>
    Polygon = 7,
    /// <summary>A star of <see cref="BinkShapeTool.StarPoints"/> points, filling the dragged rectangle.</summary>
    Star = 8,
}
