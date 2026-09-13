namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for the different parts of the <see cref="BitImage"/> component.
/// </summary>
public class BitImageClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the image.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the placeholder image element, which is only rendered while a
    /// PlaceholderSrc is provided and the image itself has not loaded yet.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the image element.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element wrapping the LoadingTemplate of the image.
    /// </summary>
    public string? LoadingTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element wrapping the ErrorTemplate of the image.
    /// </summary>
    public string? ErrorTemplate { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay element that holds the ChildContent of the image.
    /// </summary>
    public string? Content { get; set; }
}
