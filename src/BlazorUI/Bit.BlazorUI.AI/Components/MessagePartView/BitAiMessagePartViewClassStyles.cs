namespace Bit.BlazorUI;

public class BitAiMessagePartViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiMessagePartView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text the BitAiMessagePartView shows for a part of a kind it does not know.
    /// </summary>
    public string? Fallback { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of a citation part of the BitAiMessagePartView.
    /// </summary>
    public string? Citation { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiSourceCard of a citation part of the BitAiMessagePartView.
    /// </summary>
    public BitAiSourceCardClassStyles? CitationCard { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the quote of a citation part of the BitAiMessagePartView.
    /// </summary>
    public string? CitationQuote { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the locator (a page, a section) of a citation part of the BitAiMessagePartView.
    /// </summary>
    public string? CitationLocator { get; set; }
}
