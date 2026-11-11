namespace Bit.BlazorUI;

public class BitAiAgentRunViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiAgentRunView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header of the BitAiAgentRunView.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the header of the BitAiAgentRunView.
    /// </summary>
    public string? HeaderIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title (the name of the agent) of the BitAiAgentRunView.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiTokenUsageView of the header of the BitAiAgentRunView.
    /// </summary>
    public BitAiTokenUsageViewClassStyles? Usage { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiAgentStatusBadge of the header of the BitAiAgentRunView.
    /// </summary>
    public BitAiAgentStatusBadgeClassStyles? StatusBadge { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the BitAiAgentRunView.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the goal of the BitAiAgentRunView.
    /// </summary>
    public string? Goal { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the progress row (the count, the bar and the clock) of the BitAiAgentRunView.
    /// </summary>
    public string? Progress { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the count of the completed steps of the BitAiAgentRunView.
    /// </summary>
    public string? Count { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitProgress of the BitAiAgentRunView.
    /// </summary>
    public BitProgressClassStyles? ProgressBar { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the elapsed time of the BitAiAgentRunView.
    /// </summary>
    public string? Elapsed { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list of the steps of the BitAiAgentRunView.
    /// </summary>
    public string? Steps { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each item of the list of the steps of the BitAiAgentRunView.
    /// </summary>
    public string? StepItem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiAgentStepView of each step of the BitAiAgentRunView.
    /// </summary>
    public BitAiAgentStepViewClassStyles? Step { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the prompt (the question the run asks the user) of the BitAiAgentRunView.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text of the question of the BitAiAgentRunView.
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitButton of each suggested answer of the BitAiAgentRunView.
    /// </summary>
    public BitButtonClassStyles? SuggestionButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the answer row (the field and its button) of the BitAiAgentRunView.
    /// </summary>
    public string? Answer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTextField of the answer of the BitAiAgentRunView.
    /// </summary>
    public BitTextFieldClassStyles? AnswerField { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitButton sending the answer of the BitAiAgentRunView.
    /// </summary>
    public BitButtonClassStyles? AnswerButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the error of the BitAiAgentRunView.
    /// </summary>
    public string? Error { get; set; }
}
