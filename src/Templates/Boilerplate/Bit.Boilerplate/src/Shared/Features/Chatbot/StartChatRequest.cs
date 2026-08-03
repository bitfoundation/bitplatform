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
    public List<AiChatMessage> ChatMessagesHistory { get; set; } = [];
}

public enum AiChatMessageRole
{
    User,
    Assistant
}

public class AiChatMessage
{
    public AiChatMessageRole Role { get; set; }
    public string? Content { get; set; }

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
