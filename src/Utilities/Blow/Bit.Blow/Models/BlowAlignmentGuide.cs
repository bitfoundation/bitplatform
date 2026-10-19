namespace Bit.Blow;

/// <summary>Which way an alignment guide runs.</summary>
public enum BlowGuideOrientation
{
    /// <summary>A vertical line: the dragged node lines up with something on the X axis.</summary>
    Vertical,

    /// <summary>A horizontal line: the dragged node lines up with something on the Y axis.</summary>
    Horizontal,
}

/// <summary>
/// One alignment guide shown while a node is dragged: the line the node snapped to, and how far
/// it should be drawn so it visibly reaches both the dragged node and what it lined up with.
/// </summary>
/// <param name="Orientation">Whether the guide runs vertically or horizontally.</param>
/// <param name="Coordinate">Position of the line on its axis, in flow units.</param>
/// <param name="Start">Where the line begins on the other axis, in flow units.</param>
/// <param name="End">Where the line ends on the other axis, in flow units.</param>
public readonly record struct BlowAlignmentGuide(
    BlowGuideOrientation Orientation,
    double Coordinate,
    double Start,
    double End);
