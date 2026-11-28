namespace Bit.BlazorUI;

public class BitCopyButtonClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCopyButton.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button of the BitCopyButton.
    /// </summary>
    public BitButtonClassStyles? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitCopyButton, which announces the copy.
    /// </summary>
    public string? LiveRegion { get; set; }
}
