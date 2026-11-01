namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant.Data;

/// <summary>A scripted topic: its key, the resource key of its suggested prompt and of the reasoning it shows, if any.</summary>
public sealed record AssistantTopic(string Key, string PromptKey, string? ReasoningKey);
