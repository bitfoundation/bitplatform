namespace Bit.BlazorUI;

public class BitHeaderClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitHeader.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container element of the BitHeader that wraps its content.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the second row of the BitHeader, which is only rendered when an
    /// <see cref="BitHeader.ExtensionContent"/> is provided.
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the skip link of the BitHeader, which is only rendered when a
    /// <see cref="BitHeader.SkipLinkHref"/> is provided.
    /// </summary>
    public string? SkipLink { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitHeader while it is scrolled: applied once its scrolling
    /// area has travelled past the <see cref="BitHeader.ElevateOffset"/>, and taken off again back at the top.
    /// </summary>
    /// <remarks>
    /// Giving it is enough for a <see cref="BitHeader.Fixed"/> or <see cref="BitHeader.Sticky"/> header to track its
    /// scrolled state, with or without the shadow of <see cref="BitHeader.ElevateOnScroll"/>.
    /// </remarks>
    public string? Scrolled { get; set; }
}
