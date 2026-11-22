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
    /// Custom CSS classes/styles applied to the root element of the BitHeader while it is scrolled, which is
    /// while <see cref="BitHeader.IsScrolled"/> is true (an <see cref="BitHeader.ElevateOnScroll"/> header
    /// whose scrolling area has travelled past the <see cref="BitHeader.ElevateOffset"/>).
    /// On a <see cref="BitHeader.Translucent"/> header, a rule of a single class does not outweigh the translucent
    /// background, so a class restyling it there needs a heavier selector (or the scrolled CSS variables).
    /// </summary>
    public string? Scrolled { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the skip link of the BitHeader, which is only rendered when a
    /// <see cref="BitHeader.SkipLinkHref"/> is provided.
    /// </summary>
    public string? SkipLink { get; set; }
}
