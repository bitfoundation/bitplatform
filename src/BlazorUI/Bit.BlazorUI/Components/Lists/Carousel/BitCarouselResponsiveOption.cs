namespace Bit.BlazorUI;

/// <summary>
/// The layout a <see cref="BitCarousel"/> takes while it is no wider than a given breakpoint.
/// </summary>
/// <remarks>
/// The options are matched against the width of the carousel itself (not of the window), and the narrowest
/// matching one wins, so they can be listed in any order. A member that is left unset keeps the value of the
/// matching parameter of the carousel.
/// </remarks>
public class BitCarouselResponsiveOption
{
    /// <summary>
    /// The largest width (in pixels) of the carousel this option applies to.
    /// </summary>
    public double Breakpoint { get; set; }

    /// <summary>
    /// The number of items that is visible in the carousel while this option applies.
    /// </summary>
    /// <remarks>
    /// When not set, the <see cref="BitCarousel.VisibleItemsCount"/> of the carousel is used.
    /// </remarks>
    public int? VisibleItemsCount { get; set; }

    /// <summary>
    /// The number of items a navigation moves while this option applies.
    /// </summary>
    /// <remarks>
    /// When not set, the <see cref="BitCarousel.ScrollItemsCount"/> of the carousel is used.
    /// </remarks>
    public int? ScrollItemsCount { get; set; }
}
