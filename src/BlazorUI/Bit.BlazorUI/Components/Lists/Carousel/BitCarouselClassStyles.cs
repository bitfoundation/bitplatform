namespace Bit.BlazorUI;

/// <summary>
/// The custom CSS classes and styles of the different parts of the BitCarousel.
/// </summary>
public class BitCarouselClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCarousel.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the BitCarousel.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the items (slides) of the BitCarousel.
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the items (slides) of the BitCarousel that are currently on screen.
    /// </summary>
    /// <remarks>
    /// A carousel that shows several slides at a time marks every one of them, so this is the way to make
    /// the slides of the current page stand out from the ones waiting outside of the view.
    /// </remarks>
    public string? CurrentItem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the next/prev buttons of the BitCarousel.
    /// </summary>
    public string? Buttons { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icons of the next/prev buttons of the BitCarousel.
    /// </summary>
    public string? ButtonIcons { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the go to left button of the BitCarousel.
    /// </summary>
    public string? GoLeftButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the go to left button of the BitCarousel.
    /// </summary>
    public string? GoLeftButtonIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the go to right button of the BitCarousel.
    /// </summary>
    public string? GoRightButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the go to right button of the BitCarousel.
    /// </summary>
    public string? GoRightButtonIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dots container of the BitCarousel.
    /// </summary>
    public string? DotsContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dot elements of the BitCarousel.
    /// </summary>
    public string? Dots { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the current dot element of the BitCarousel.
    /// </summary>
    [Obsolete($"This property is misspelled and will be removed in a future release. Use {nameof(CurrentDot)} instead.")]
    public string? CurrectDot { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the current dot element of the BitCarousel.
    /// </summary>
    public string? CurrentDot { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the play/pause button of the BitCarousel.
    /// </summary>
    public string? PlayPauseButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the play/pause button of the BitCarousel.
    /// </summary>
    public string? PlayPauseButtonIcon { get; set; }



    /// <summary>
    /// Resolves the current dot value, falling back to the misspelled property so the values
    /// assigned to it before it was replaced keep being applied.
    /// </summary>
    internal string? GetCurrentDot()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return CurrentDot ?? CurrectDot;
#pragma warning restore CS0618
    }
}
