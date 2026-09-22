namespace Bit.BlazorUI;

/// <summary>
/// Defines per-part CSS class/style values for <see cref="BitButton"/>.
/// </summary>
public class BitButtonClassStyles
{
    /// <summary>
    /// Custom class or style applied to the root element.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom class or style applied to the icon element (the glyph, or the image rendered for IconUrl).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom class or style applied to the column that holds the primary and secondary lines of text.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Custom class or style applied to the primary line of text.
    /// </summary>
    public string? Primary { get; set; }

    /// <summary>
    /// Custom class or style applied to the secondary line of text.
    /// </summary>
    public string? Secondary { get; set; }

    /// <summary>
    /// Custom class or style applied to the wrapper of the content that keeps the button size while it is hidden in the loading state.
    /// </summary>
    public string? HiddenContent { get; set; }

    /// <summary>
    /// Custom class or style applied to the container of the spinner and its label in the loading state.
    /// </summary>
    public string? LoadingContainer { get; set; }

    /// <summary>
    /// Custom class or style applied to the loading spinner element.
    /// </summary>
    public string? Spinner { get; set; }

    /// <summary>
    /// Custom class or style applied to the loading label element.
    /// </summary>
    public string? LoadingLabel { get; set; }
}
