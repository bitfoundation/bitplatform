namespace Bit.BlazorUI;

public class BitAiMessageActionsClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiMessageActions.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the group of action buttons of the BitAiMessageActions.
    /// </summary>
    public string? Actions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the read aloud button of the BitAiMessageActions.
    /// </summary>
    public BitButtonClassStyles? ReadAloudButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the status of the BitAiMessageActions saying that reading the message aloud failed.
    /// </summary>
    public string? ReadAloudStatus { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the regenerate button of the BitAiMessageActions.
    /// </summary>
    public BitButtonClassStyles? RegenerateButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the two feedback buttons of the BitAiMessageActions.
    /// </summary>
    public BitButtonClassStyles? FeedbackButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the model name of the BitAiMessageActions.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the time of the BitAiMessageActions.
    /// </summary>
    public string? Time { get; set; }
}
