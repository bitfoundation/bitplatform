namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>What the user answered a card with (See <c>SharedAppMessages.AWAIT_AI_CHAT_CARD</c>).</summary>
public static class AiChatCardDecision
{
    public const string Approved = nameof(Approved);

    public const string Declined = nameof(Declined);

    /// <summary>The wait ended without an answer: the user moved on, or the conversation or the call ended.</summary>
    public const string NoAnswer = nameof(NoAnswer);
}
