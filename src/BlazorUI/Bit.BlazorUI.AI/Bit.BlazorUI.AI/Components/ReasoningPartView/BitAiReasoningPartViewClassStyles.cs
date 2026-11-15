namespace Bit.BlazorUI;

public class BitAiReasoningPartViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiReasoningPartView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiCollapsible that is the panel of the BitAiReasoningPartView.
    /// </summary>
    public BitAiCollapsibleClassStyles? Collapsible { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content of the header of the BitAiReasoningPartView: its icon, its title and
    /// its loading dots.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon in the header of the BitAiReasoningPartView.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title in the header of the BitAiReasoningPartView.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the loading dots the header of the BitAiReasoningPartView shows while the model
    /// is still reasoning.
    /// </summary>
    public BitLoadingClassStyles? Loading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the scrolling body of the BitAiReasoningPartView, which holds the reasoning.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiStreamingMarkdown the BitAiReasoningPartView renders the reasoning with.
    /// </summary>
    public BitAiStreamingMarkdownClassStyles? Markdown { get; set; }
}
