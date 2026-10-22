namespace Bit.BlazorUI;

public class BitAiAgentStepViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiAgentStepView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the timeline marker of the BitAiAgentStepView.
    /// </summary>
    public string? Marker { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the check icon of the marker of a completed BitAiAgentStepView.
    /// </summary>
    public string? MarkerIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body (everything beside the marker) of the BitAiAgentStepView.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title row of the BitAiAgentStepView.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the duration (or the time elapsed so far) of the BitAiAgentStepView.
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the detail of the BitAiAgentStepView.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the summary of the BitAiAgentStepView.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list of tool calls of the BitAiAgentStepView.
    /// </summary>
    public string? ToolCalls { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each item of the list of tool calls of the BitAiAgentStepView.
    /// </summary>
    public string? ToolCall { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTag of each tool call of the BitAiAgentStepView.
    /// </summary>
    public BitTagClassStyles? ToolCallTag { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the error message of the BitAiAgentStepView.
    /// </summary>
    public string? Error { get; set; }
}
