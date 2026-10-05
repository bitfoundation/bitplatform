namespace Bit.BlazorUI;

/// <summary>
/// Defines where the content is placed. Start and End follow the text direction; Left and Right stay on their side.
/// </summary>
public enum BitPosition
{
    /// <summary>
    /// At the top, against the left edge.
    /// </summary>
    TopLeft,

    /// <summary>
    /// At the top, centered horizontally.
    /// </summary>
    TopCenter,

    /// <summary>
    /// At the top, against the right edge.
    /// </summary>
    TopRight,

    /// <summary>
    /// At the top, against the start edge (the left in left-to-right).
    /// </summary>
    TopStart,

    /// <summary>
    /// At the top, against the end edge (the right in left-to-right).
    /// </summary>
    TopEnd,

    /// <summary>
    /// Centered vertically, against the left edge.
    /// </summary>
    CenterLeft,

    /// <summary>
    /// Centered on both axes.
    /// </summary>
    Center,

    /// <summary>
    /// Centered vertically, against the right edge.
    /// </summary>
    CenterRight,

    /// <summary>
    /// Centered vertically, against the start edge (the left in left-to-right).
    /// </summary>
    CenterStart,

    /// <summary>
    /// Centered vertically, against the end edge (the right in left-to-right).
    /// </summary>
    CenterEnd,

    /// <summary>
    /// At the bottom, against the left edge.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// At the bottom, centered horizontally.
    /// </summary>
    BottomCenter,

    /// <summary>
    /// At the bottom, against the right edge.
    /// </summary>
    BottomRight,

    /// <summary>
    /// At the bottom, against the start edge (the left in left-to-right).
    /// </summary>
    BottomStart,

    /// <summary>
    /// At the bottom, against the end edge (the right in left-to-right).
    /// </summary>
    BottomEnd
}
