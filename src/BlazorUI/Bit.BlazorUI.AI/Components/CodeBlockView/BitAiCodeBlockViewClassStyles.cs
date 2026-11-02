namespace Bit.BlazorUI;

public class BitAiCodeBlockViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiCodeBlockView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header bar of the BitAiCodeBlockView, which holds the language and the copy button.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the language (or file name) label in the header of the BitAiCodeBlockView.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the copy button in the header of the BitAiCodeBlockView.
    /// </summary>
    public BitAiCopyButtonClassStyles? CopyButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the scroll container of the code of the BitAiCodeBlockView.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the pre element of the BitAiCodeBlockView.
    /// </summary>
    public string? Pre { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the code element of the BitAiCodeBlockView.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every numbered line of the BitAiCodeBlockView (rendered only while
    /// <see cref="BitAiUiOptions.ShowLineNumbers"/> is on).
    /// </summary>
    public string? Line { get; set; }
}
