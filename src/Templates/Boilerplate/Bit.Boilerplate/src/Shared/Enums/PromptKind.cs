namespace Boilerplate.Shared.Enums;

public enum PromptKind
{
    /// <summary>
    /// The system prompt is used to instruct the AI on how to behave as a support agent for the app users.
    /// </summary>
    Support,
    /// <summary>
    /// Every 5 questions, the AI will summarize the chat history in order to reduce the costs of the ongoing conversation.
    /// </summary>
    SummarizeConversationContext
}
