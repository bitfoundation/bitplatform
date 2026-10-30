namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant.Data;

public sealed class AssistantMessage
{
    public required AssistantRole Role { get; init; }

    public required DateTimeOffset At { get; init; }

    public AssistantTopic? Topic { get; init; }

    public string Text { get; set; } = string.Empty;

    public AssistantCardKind Card { get; set; }

    public bool IsStreaming { get; set; }

    public bool IsFailed { get; set; }

    public bool? IsHelpful { get; set; }
}
