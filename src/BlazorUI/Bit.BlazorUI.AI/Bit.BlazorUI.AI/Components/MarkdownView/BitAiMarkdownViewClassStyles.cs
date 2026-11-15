namespace Bit.BlazorUI;

public class BitAiMarkdownViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiMarkdownView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every paragraph of the BitAiMarkdownView.
    /// </summary>
    public string? Paragraph { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every heading of the BitAiMarkdownView.
    /// </summary>
    public string? Heading { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every list (bulleted, numbered or task list) of the BitAiMarkdownView.
    /// </summary>
    public string? List { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every list item of the BitAiMarkdownView.
    /// </summary>
    public string? ListItem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every block quote of the BitAiMarkdownView.
    /// </summary>
    public string? Quote { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the scroll container of every table of the BitAiMarkdownView.
    /// </summary>
    public string? TableContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every table of the BitAiMarkdownView.
    /// </summary>
    public string? Table { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every thematic break (horizontal rule) of the BitAiMarkdownView.
    /// </summary>
    public string? Separator { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every inline code span of the BitAiMarkdownView.
    /// </summary>
    public string? InlineCode { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every link of the BitAiMarkdownView.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every image of the BitAiMarkdownView.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button the BitAiMarkdownView shows in place of an image the image policy
    /// loads only on request.
    /// </summary>
    public BitActionButtonClassStyles? ImagePlaceholder { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the alternative text the BitAiMarkdownView shows in place of an image the image
    /// policy blocks.
    /// </summary>
    public string? BlockedImage { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the built-in rendering of every mathematical expression of the BitAiMarkdownView.
    /// </summary>
    public string? Math { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every footnote reference of the BitAiMarkdownView.
    /// </summary>
    public string? FootnoteReference { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footnotes section of the BitAiMarkdownView.
    /// </summary>
    public string? Footnotes { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the caret the BitAiMarkdownView draws after streaming text.
    /// </summary>
    public string? Caret { get; set; }
}
