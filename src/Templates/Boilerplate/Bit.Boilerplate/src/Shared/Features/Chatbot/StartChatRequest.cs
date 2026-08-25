namespace Boilerplate.Shared.Features.Chatbot;

public class StartChatRequest
{
    public int? CultureId { get; set; }

    public string? DeviceInfo { get; set; }

    public string? TimeZoneId { get; set; }

    /// <summary>
    /// On chat restart (e.g., SignalR reconnection or chat panel close),
    /// Server's AppHub releases chat related resources including chat history.
    /// When the chat panel is reopened, the client must resend the chat history to the server.
    /// </summary>
    public List<AiChatMessageResponse> ChatMessagesHistory { get; set; } = [];
}

public enum AiChatMessageRole
{
    User,
    Assistant
}

/// <summary>
/// What the panel sends when the user asks something, and deliberately nothing else: the role of an incoming message
/// is always the user's, so carrying one would only offer a caller a way to claim the assistant said something.
/// </summary>
public class AiChatMessageRequest
{
    public string? Content { get; set; }

    /// <inheritdoc cref="AiChatMessageResponse.AttachmentId"/>
    public Guid? AttachmentId { get; set; }
}

/// <summary>
/// One message of the conversation as the panel holds it: what it renders, and what it resends as history when the
/// connection is restarted. <see cref="AiChatMessageRequest"/> plus who said it and whether it finished.
/// </summary>
public class AiChatMessageResponse
{
    public AiChatMessageRole Role { get; set; }

    public string? Content { get; set; }

    /// <summary>
    /// The image attached to this message, as the id it was stored under, or null when it carries none. Both ends
    /// already know the kind is <c>AiChatImage</c> and the route it is served from, so the id is all that travels.
    /// </summary>
    public Guid? AttachmentId { get; set; }

    /// <summary>
    /// False for an answer that was cancelled or failed mid-stream. The client keeps such a message on screen
    /// (tagged as canceled), but the server drops it from the history it sends to the model, so a truncated
    /// sentence is never replayed as a complete previous answer.
    /// </summary>
    public bool Successful { get; set; } = true;
}

public class AiChatFollowUpList
{
    public List<string> FollowUpSuggestions { get; set; } = [];
}
