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
    /// Custom CSS classes/styles for the skip link of the BitHeader, which is only rendered when a
    /// <see cref="BitHeader.SkipLinkHref"/> is provided.
    /// </summary>
    public string? SkipLink { get; set; }
}
