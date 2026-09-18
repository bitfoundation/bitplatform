namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for the different parts of the <see cref="BitFlag"/> component.
/// </summary>
public class BitFlagClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the flag, which is the frame the flag is
    /// drawn in - it carries the size, the shape and the border.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the img element of the flag, which is only rendered while the
    /// flag is drawn as an image rather than as an emoji.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element the emoji flag is written in, which is only rendered
    /// while the flag is drawn as an emoji.
    /// </summary>
    public string? Emoji { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element wrapping the FallbackTemplate of the flag, which is
    /// only rendered while there is no flag to draw.
    /// </summary>
    public string? Fallback { get; set; }
}
