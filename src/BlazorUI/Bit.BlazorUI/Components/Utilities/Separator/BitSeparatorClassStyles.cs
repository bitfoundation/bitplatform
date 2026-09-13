namespace Bit.BlazorUI;

/// <summary>
/// Custom CSS classes/styles for the different parts of the <see cref="BitSeparator"/> component.
/// </summary>
public class BitSeparatorClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the separator.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element wrapping the ChildContent of the separator, which is
    /// only rendered while the separator has content.
    /// </summary>
    public string? Content { get; set; }
}
