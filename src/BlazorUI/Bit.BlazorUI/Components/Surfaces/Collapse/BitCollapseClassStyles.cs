namespace Bit.BlazorUI;

public class BitCollapseClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCollapse.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCollapse in the expanded state.
    /// </summary>
    public string? Expanded { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCollapse in the collapsed state.
    /// </summary>
    public string? Collapsed { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content region of the BitCollapse, which is the element that animates
    /// between the two states.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the wrapper the BitCollapse puts around its content, which is the element
    /// that carries the padding and clips what is outside the collapsed size.
    /// </summary>
    public string? Wrapper { get; set; }
}
