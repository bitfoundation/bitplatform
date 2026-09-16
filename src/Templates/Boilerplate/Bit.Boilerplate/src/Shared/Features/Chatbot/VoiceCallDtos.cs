namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>See <c>ChatbotController.StartVoiceCall</c>.</summary>
public class StartVoiceCallRequestDto
{
    /// <summary>The browser's WebRTC offer.</summary>
    [Required(AllowEmptyStrings = false), StringLength(maximumLength: 64 * 1024)]
    public string Sdp { get; set; } = default!;

    public int? CultureId { get; set; }

    [StringLength(maximumLength: 64)]
    public string? TimeZoneId { get; set; }

    [StringLength(maximumLength: 64)]
    public string? DeviceInfo { get; set; }

    /// <summary>The conversation so far, trusted and trimmed as the text chat's is (See <c>AppChatbot.BelievableHistory</c>).</summary>
    public List<AiChatMessage> ChatMessagesHistory { get; set; } = [];
}

public class StartVoiceCallResponseDto
{
    /// <summary>The provider's WebRTC answer.</summary>
    public string Sdp { get; set; } = default!;

    /// <summary>The server hangs up once the call reaches this.</summary>
    public TimeSpan MaxDuration { get; set; }
}
