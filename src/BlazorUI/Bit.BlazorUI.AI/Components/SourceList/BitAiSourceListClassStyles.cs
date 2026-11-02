namespace Bit.BlazorUI;

public class BitAiSourceListClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiSourceList.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the heading of the BitAiSourceList.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list (the grid of the cards) of the BitAiSourceList.
    /// </summary>
    public string? List { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every BitAiSourceCard of the BitAiSourceList.
    /// </summary>
    public BitAiSourceCardClassStyles? SourceCard { get; set; }
}
