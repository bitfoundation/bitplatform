namespace Bit.BlazorUI;

/// <summary>
/// The read-only view of a message that transports and persistence see.
/// </summary>
/// <remarks>
/// The mutable, observable implementation is <see cref="BitAiMessage"/>. Transports depend on this
/// interface only, so a transport package never has to reference the state layer.
/// </remarks>
public interface IBitAiMessage
{
    /// <summary>Stable identifier, unique within its conversation.</summary>
    string MessageId { get; }

    /// <summary>Who authored the message.</summary>
    BitAiRole Role { get; }

    /// <summary>The ordered parts that make up the message.</summary>
    IReadOnlyList<IBitAiMessagePart> Parts { get; }

    /// <summary>Files carried by the message.</summary>
    IReadOnlyList<BitAiAttachment> Attachments { get; }

    /// <summary>Where the message is in its lifecycle.</summary>
    BitAiMessageStatus Status { get; }

    /// <summary>When the message was created.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>How the message was generated, for assistant messages.</summary>
    BitAiGenerationMetadata? Metadata { get; }

    /// <summary>
    /// The plain-text projection of the message, concatenating every <see cref="IBitAiTextualPart"/>.
    /// </summary>
    /// <returns>The message as text.</returns>
    string GetText();
}

/// <summary>
/// The read-only view of a conversation that transports and persistence see.
/// </summary>
public interface IBitAiConversation
{
    /// <summary>Stable identifier.</summary>
    string ConversationId { get; }

    /// <summary>The title shown in a conversation list.</summary>
    string? Title { get; }

    /// <summary>The messages, in order.</summary>
    IReadOnlyList<IBitAiMessage> Messages { get; }

    /// <summary>When the conversation was created.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>When the conversation last changed.</summary>
    DateTimeOffset UpdatedAt { get; }
}
