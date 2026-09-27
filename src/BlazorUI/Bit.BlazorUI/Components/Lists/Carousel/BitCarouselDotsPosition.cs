namespace Bit.BlazorUI;

/// <summary>
/// Where the dots of a <see cref="BitCarousel"/> are placed around its slides.
/// </summary>
public enum BitCarouselDotsPosition
{
    /// <summary>
    /// Below the slides.
    /// </summary>
    Bottom,

    /// <summary>
    /// Above the slides.
    /// </summary>
    Top,

    /// <summary>
    /// In a column beside the slides, at the start of the reading direction (the left in left-to-right).
    /// </summary>
    Start,

    /// <summary>
    /// In a column beside the slides, at the end of the reading direction (the right in left-to-right).
    /// </summary>
    End
}
