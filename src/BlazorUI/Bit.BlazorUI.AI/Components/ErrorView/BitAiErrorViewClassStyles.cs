namespace Bit.BlazorUI;

public class BitAiErrorViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiErrorView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitMessage the BitAiErrorView draws the error with.
    /// </summary>
    public BitMessageClassStyles? Message { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text of the error of the BitAiErrorView.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the line saying how long to wait before retrying, of the BitAiErrorView.
    /// </summary>
    public string? RetryAfter { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the technical detail of the BitAiErrorView.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row of the action buttons of the BitAiErrorView.
    /// </summary>
    public string? Actions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the retry button of the BitAiErrorView.
    /// </summary>
    public BitButtonClassStyles? RetryButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button disclosing the technical detail of the BitAiErrorView.
    /// </summary>
    public BitButtonClassStyles? DetailButton { get; set; }
}
