namespace Boilerplate.Shared.Dtos.Chatbot;

public class StartChatbotRequest
{
    public int CultureId { get; set; }

    public string? DeviceInfo { get; set; }

    /// <summary>
    /// On chat restart (e.g., SignalR reconnection or chat panel close), 
    /// Server's AppHub releases chat related resources including chat history. 
    /// When the chat panel is reopened, the client must resend the chat history to the server.
    /// </summary>
    public List<AiChatMessage> ChatMessagesHistory { get; set; } = [];

    public Uri? ServerApiAddress { get; set; } // Getting the api address in ChatBot Hub has some complexities, specially when using Azure SignalR or being behind a reverse proxy, so we pass it from the client side.
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

    [JsonIgnore]
    public bool Successful { get; set; } = true;
}
