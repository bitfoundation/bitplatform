namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant.Data;

/// <summary>A chat the user kept: its conversation id and the conversation as the AI components' own compact JSON.</summary>
public sealed record AssistantChat(string ConversationId, string Json);
