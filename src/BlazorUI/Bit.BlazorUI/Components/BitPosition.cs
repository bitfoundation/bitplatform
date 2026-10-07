namespace Bit.BlazorUI;

/// <summary>
/// A point on the three-by-three grid of the area a component is positioned in, used wherever a
/// component sits somewhere inside a box rather than against one of its edges.
/// </summary>
/// <remarks>
/// The Start and End values are logical and are what a component following the reading direction of its
/// content wants: Start is the left in an LTR context and the right in an RTL one. The Left and Right
/// values are physical and stay on the same side of the screen in both reading directions.
/// </remarks>
public enum BitPosition
{
    /// <summary>
    /// The top left corner, in both reading directions.
    /// </summary>
    TopLeft,

    /// <summary>
    /// The top edge, centered horizontally.
    /// </summary>
    TopCenter,

    /// <summary>
    /// The top right corner, in both reading directions.
    /// </summary>
    TopRight,

    /// <summary>
    /// The top edge, on the side the reading direction starts from.
    /// </summary>
    TopStart,

    /// <summary>
    /// The top edge, on the side the reading direction ends at.
    /// </summary>
    TopEnd,

    /// <summary>
    /// The left edge, centered vertically, in both reading directions.
    /// </summary>
    CenterLeft,

    /// <summary>
    /// Centered both ways.
    /// </summary>
    Center,

    /// <summary>
    /// The right edge, centered vertically, in both reading directions.
    /// </summary>
    CenterRight,

    /// <summary>
    /// Centered vertically, on the side the reading direction starts from.
    /// </summary>
    CenterStart,

    /// <summary>
    /// Centered vertically, on the side the reading direction ends at.
    /// </summary>
    CenterEnd,

    /// <summary>
    /// The bottom left corner, in both reading directions.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// The bottom edge, centered horizontally.
    /// </summary>
    BottomCenter,

    /// <summary>
    /// The bottom right corner, in both reading directions.
    /// </summary>
    BottomRight,

    /// <summary>
    /// The bottom edge, on the side the reading direction starts from.
    /// </summary>
    BottomStart,

    /// <summary>
    /// The bottom edge, on the side the reading direction ends at.
    /// </summary>
    BottomEnd
}
