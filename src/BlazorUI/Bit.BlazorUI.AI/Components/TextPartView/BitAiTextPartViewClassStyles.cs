namespace Bit.BlazorUI;

public class BitAiTextPartViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiTextPartView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiStreamingMarkdown the BitAiTextPartView renders its text with.
    /// </summary>
    public BitAiStreamingMarkdownClassStyles? Markdown { get; set; }
}
