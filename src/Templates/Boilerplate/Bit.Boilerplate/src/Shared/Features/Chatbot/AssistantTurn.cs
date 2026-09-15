namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// Everything the server sends for one turn, as a single json document - the chat stream carries nothing else, so the
/// panel reads the document rather than routing frames by kind (See <c>AppAiChatPanel.RunChannel</c> and
/// <see cref="PartialJsonReader{T}"/>).
/// <para>
/// The server writes the opening, splices the model's reply in as it arrives, then writes the closing (See
/// <c>AppChatbot.ProcessNewMessage</c>). Property order is streaming order and is load bearing:
/// <see cref="Signature"/> and <see cref="Successful"/> aren't knowable until the end.
/// </para>
/// </summary>
public class AssistantTurn
{
    /// <summary>When the server began the turn, by its own clock rather than the device's.</summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>What the model wrote. Null on a turn that failed before it wrote anything.</summary>
    public AssistantReply? Reply { get; set; }

    /// <inheritdoc cref="AiChatMessage.Signature"/>
    public string? Signature { get; set; }

    /// <inheritdoc cref="AiChatMessage.Successful"/>
    public bool Successful { get; set; } = true;
}
