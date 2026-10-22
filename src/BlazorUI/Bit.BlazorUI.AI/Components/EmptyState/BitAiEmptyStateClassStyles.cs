namespace Bit.BlazorUI;

public class BitAiEmptyStateClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiEmptyState.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the BitAiEmptyState.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the heading of the BitAiEmptyState.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the supporting line of the BitAiEmptyState.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the suggestions of the BitAiEmptyState.
    /// </summary>
    public string? Suggestions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each suggestion button of the BitAiEmptyState.
    /// </summary>
    public BitButtonClassStyles? Suggestion { get; set; }
}
