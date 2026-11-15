namespace Bit.BlazorUI;

public class BitEmptyStateClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitEmptyState.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the BitEmptyState, or the element its IconTemplate is rendered in.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of the BitEmptyState.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the line under the title of the BitEmptyState.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the element the actions (ChildContent) of the BitEmptyState are rendered in.
    /// </summary>
    public string? Content { get; set; }
}
