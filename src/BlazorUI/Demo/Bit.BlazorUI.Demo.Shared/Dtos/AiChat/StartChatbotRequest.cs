namespace Bit.BlazorUI.Demo.Shared.Dtos.AiChat;

public class StartChatbotRequest
{
    public string? DeviceInfo { get; set; }

    public List<AiChatMessage> ChatMessagesHistory { get; set; } = [];
}
