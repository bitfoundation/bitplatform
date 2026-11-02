namespace Bit.BlazorUI;

public class BitAiStreamingMarkdownClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiStreamingMarkdown.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the parts of the BitAiMarkdownView the BitAiStreamingMarkdown renders its text with.
    /// </summary>
    public BitAiMarkdownViewClassStyles? Markdown { get; set; }
}
