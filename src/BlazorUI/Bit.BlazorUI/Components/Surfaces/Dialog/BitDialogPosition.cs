namespace Bit.BlazorUI;

/// <summary>
/// Where the Dialog sits inside the area it covers.
/// </summary>
/// <remarks>
/// The Left and Right values are physical: they stay on the same side of the screen in both reading
/// directions. The Start and End values are logical: Start is the left in an LTR Dialog and the right
/// in an RTL one, which is what a Dialog that follows the reading direction of its content wants.
/// </remarks>
public enum BitDialogPosition
{
    /// <summary>
    /// Centered both ways, which is where a Dialog goes unless there is a reason for it to be elsewhere.
    /// </summary>
    Center,

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
    /// The left edge, centered vertically, in both reading directions.
    /// </summary>
    CenterLeft,

    /// <summary>
    /// The right edge, centered vertically, in both reading directions.
    /// </summary>
    CenterRight,

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
    /// The top edge, on the side the reading direction starts from.
    /// </summary>
    TopStart,

    /// <summary>
    /// The top edge, on the side the reading direction ends at.
    /// </summary>
    TopEnd,

    /// <summary>
    /// Centered vertically, on the side the reading direction starts from.
    /// </summary>
    CenterStart,

    /// <summary>
    /// Centered vertically, on the side the reading direction ends at.
    /// </summary>
    CenterEnd,

    /// <summary>
    /// The bottom edge, on the side the reading direction starts from.
    /// </summary>
    BottomStart,

    /// <summary>
    /// The bottom edge, on the side the reading direction ends at.
    /// </summary>
    BottomEnd
}
