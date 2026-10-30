namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant.Data;

public sealed class AssistantConversation
{
    public required string Id { get; init; }

    public required string Title { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<AssistantMessage> Messages { get; init; } = [];
}
