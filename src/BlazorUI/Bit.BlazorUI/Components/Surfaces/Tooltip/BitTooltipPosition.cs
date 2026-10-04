namespace Bit.BlazorUI;

/// <summary>
/// Where the tooltip is placed around its anchor. The sides are the sides of the screen, not of the reading
/// order; <see cref="BitTooltip.MirrorInRtl"/> swaps left and right in a right-to-left tooltip.
/// </summary>
public enum BitTooltipPosition
{
    /// <summary>
    /// Above the anchor, centered on it.
    /// </summary>
    Top,

    /// <summary>
    /// Above the anchor, off its left corner.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Above the anchor, off its right corner.
    /// </summary>
    TopRight,

    /// <summary>
    /// Right of the anchor, off its top corner.
    /// </summary>
    RightTop,

    /// <summary>
    /// Right of the anchor, centered on it.
    /// </summary>
    Right,

    /// <summary>
    /// Right of the anchor, off its bottom corner.
    /// </summary>
    RightBottom,

    /// <summary>
    /// Below the anchor, off its right corner.
    /// </summary>
    BottomRight,

    /// <summary>
    /// Below the anchor, centered on it.
    /// </summary>
    Bottom,

    /// <summary>
    /// Below the anchor, off its left corner.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Left of the anchor, off its bottom corner.
    /// </summary>
    LeftBottom,

    /// <summary>
    /// Left of the anchor, centered on it.
    /// </summary>
    Left,

    /// <summary>
    /// Left of the anchor, off its top corner.
    /// </summary>
    LeftTop
}
