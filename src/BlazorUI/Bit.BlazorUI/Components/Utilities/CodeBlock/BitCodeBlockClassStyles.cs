namespace Bit.BlazorUI;

public class BitCodeBlockClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitCodeBlock.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header bar of the BitCodeBlock, which holds the title, the language and the copy
    /// button (rendered when a Title or a Language is set, or the block is a Terminal).
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the three window dots of the header of a Terminal BitCodeBlock.
    /// </summary>
    public string? Dots { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title in the header of the BitCodeBlock.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the language label in the header of the BitCodeBlock.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the copy button of the BitCodeBlock.
    /// </summary>
    public string? CopyButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the scroll container of the code of the BitCodeBlock.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the pre element of the BitCodeBlock.
    /// </summary>
    public string? Pre { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the code element of the BitCodeBlock.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitCodeBlock that announces a copy.
    /// </summary>
    public string? LiveRegion { get; set; }
}
