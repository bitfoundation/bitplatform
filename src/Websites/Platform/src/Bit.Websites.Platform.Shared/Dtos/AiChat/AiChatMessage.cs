namespace Bit.Websites.Platform.Shared.Dtos.AiChat;

public class AiChatMessage
{
    public AiChatMessageRole Role { get; set; }

    public string? Content { get; set; }

    [JsonIgnore]
    public bool Successful { get; set; } = true;
}
