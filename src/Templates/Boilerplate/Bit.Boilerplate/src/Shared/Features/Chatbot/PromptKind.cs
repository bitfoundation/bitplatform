namespace Boilerplate.Shared.Features.Chatbot;

public enum PromptKind
{
    /// <summary>
    /// The system prompt is used to instruct the AI on how to behave as a support agent for the app users.
    /// </summary>
    Support,

    /// <summary>
    /// Analyzes product images to ensure they meet catalog standards for car products
    /// </summary>
    AnalyzeProductImage,

    /// <summary>
    /// Generates follow-up suggestions based on user interactions
    /// </summary>
    FollowUpSuggestion
}
