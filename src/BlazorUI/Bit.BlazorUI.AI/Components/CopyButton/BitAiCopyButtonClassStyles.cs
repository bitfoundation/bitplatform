namespace Bit.BlazorUI;

public class BitAiCopyButtonClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiCopyButton.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button of the BitAiCopyButton.
    /// </summary>
    public BitButtonClassStyles? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitAiCopyButton, which announces the copy.
    /// </summary>
    public string? LiveRegion { get; set; }
}
