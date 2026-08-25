namespace Bit.BlazorUI;

/// <summary>
/// Determines the shape of the placeholder the <see cref="BitShimmer"/> draws.
/// </summary>
public enum BitShimmerShape
{
    /// <summary>
    /// A rectangle with the small corner radius of the theme, which is what a line of text or a block of content reads as.
    /// </summary>
    Rounded,

    /// <summary>
    /// A rectangle with no corner radius at all, for content that meets its container edge to edge.
    /// </summary>
    Square,

    /// <summary>
    /// A rectangle with fully rounded ends, which is what a button, a tag or a chip reads as.
    /// </summary>
    Pill,

    /// <summary>
    /// A circle, which is what an avatar or a round icon reads as. It takes its diameter from whichever of the
    /// height and the width is set, and ignores <see cref="BitShimmer.Lines"/>.
    /// </summary>
    Circle
}
