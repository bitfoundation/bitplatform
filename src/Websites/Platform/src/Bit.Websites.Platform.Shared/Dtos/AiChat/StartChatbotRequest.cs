namespace Bit.Websites.Platform.Shared.Dtos.AiChat;

public class StartChatbotRequest
{
    public int CultureId { get; set; }

    public string? DeviceInfo { get; set; }

    public List<AiChatMessage> ChatMessagesHistory { get; set; } = [];

    public Uri? ServerApiAddress { get; set; }
}
