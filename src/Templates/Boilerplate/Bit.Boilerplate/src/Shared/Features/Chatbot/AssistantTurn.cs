namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// Everything the server sends for one turn, as a single json document - the chat stream carries nothing else, so the
/// panel reads the document rather than routing frames by kind (See <c>AppAiChatPanel.RunChannel</c> and
/// <see cref="PartialJsonReader{T}"/>).
/// <para>
/// The server writes the opening, the answer as the model writes it, then the closing (See
/// <c>AppChatbot.ProcessNewMessage</c>). Property order is streaming order and is load bearing:
/// <see cref="Signature"/> and <see cref="Successful"/> aren't knowable until the end.
/// </para>
/// </summary>
public class AssistantTurn
{
    /// <summary>When the server began the turn, by its own clock rather than the device's.</summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>What the model wrote, as markdown: what the user is shown, and what gets signed and resent.</summary>
    public string? Answer { get; set; }

    /// <inheritdoc cref="AiChatMessage.Signature"/>
    public string? Signature { get; set; }

    /// <inheritdoc cref="AiChatMessage.Successful"/>
    public bool Successful { get; set; } = true;
}
