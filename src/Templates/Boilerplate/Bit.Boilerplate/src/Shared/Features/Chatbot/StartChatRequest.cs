namespace Boilerplate.Shared.Features.Chatbot;

public class StartChatRequest
{
    /// <summary>
    /// How many of the newest messages the model is shown, and so all the client has any use for: the server drops
    /// everything older on the way in (See <c>AppChatbot.TrimChatHistory</c>).
    /// </summary>
    public const int MaxChatMessagesHistory = 40;

    public int? CultureId { get; set; }

    public string? DeviceInfo { get; set; }

    public string? TimeZoneId { get; set; }

    /// <summary>
    /// On chat restart (e.g., SignalR reconnection or chat panel close),
    /// Server's AppHub releases chat related resources including chat history.
    /// When the chat panel is reopened, the client must resend the chat history to the server.
    /// </summary>
    public List<AiChatMessage> ChatMessagesHistory { get; set; } = [];
}

public enum AiChatMessageRole
{
    User,
    Assistant
}

/// <summary>
/// One message of the conversation, whoever said it: what the panel renders, what it resends as history on restart,
/// and what it sends when the user asks something. That last one is why <see cref="Role"/> and
/// <see cref="Signature"/> aren't trusted on the way in - everything arriving on the chat stream is the user speaking
/// (See <c>AppChatbot.ProcessNewMessage</c>), and a resent history is believed only where the signature checks out
/// (See <c>AppChatbot.WrittenByThisAssistantOrByTheUser</c>).
/// </summary>
public class AiChatMessage
{
    public AiChatMessageRole Role { get; set; }

    public string? Content { get; set; }

    /// <summary>
    /// When the server began writing this, by its own clock (See <see cref="AssistantTurn.SentAt"/>). Default on the
    /// user's messages and on the panel's greeting.
    /// </summary>
    public DateTimeOffset SentAt { get; set; }

    /// <summary>
    /// The image the user attached, as the id it was stored under. Both ends know the kind is <c>AiChatImage</c> and
    /// the route it is served from, so only the id travels. The assistant never attaches one.
    /// </summary>
    public Guid? AttachmentId { get; set; }

    /// <summary>
    /// False for an answer that was cancelled or failed mid-stream. The client keeps such a message on screen
    /// (tagged as canceled), but the server drops it from the history it sends to the model, so a truncated
    /// sentence is never replayed as a complete previous answer.
    /// </summary>
    public bool Successful { get; set; } = true;

    /// <summary>
    /// The server's signature over <see cref="Content"/>, carried on the turn that wrote it (See
    /// <see cref="AssistantTurn.Signature"/>). Null on anything the assistant didn't write; an assistant message that
    /// comes back without a matching one is dropped from the resent history.
    /// </summary>
    public string? Signature { get; set; }
}
