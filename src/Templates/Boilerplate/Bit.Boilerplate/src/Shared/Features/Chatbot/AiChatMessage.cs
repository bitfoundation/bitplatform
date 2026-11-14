namespace Boilerplate.Shared.Features.Chatbot;

public enum AiChatMessageRole
{
    User,
    Assistant
}

/// <summary>
/// One message of the conversation a voice call carries on (See <see cref="StartVoiceCallRequestDto.ChatMessagesHistory"/>).
/// The text chat sends its conversation in Bit.BlazorUI.AI's own wire format instead (See <see cref="AiChatProtocol"/>).
/// <para>
/// <see cref="Role"/> and <see cref="Signature"/> aren't trusted on the way in: an assistant message is the assistant's only
/// where the signature checks out, and anything else is replayed to the model as the user's (See <c>AppChatbot.AsProvablySaid</c>).
/// </para>
/// </summary>
public class AiChatMessage
{
    public const int MaxContentLength = 10_000;

    public AiChatMessageRole Role { get; set; }

    [StringLength(MaxContentLength)]
    public string? Content { get; set; }

    /// <summary>The server's signature over <see cref="Content"/> (See <see cref="AiChatProtocol.Signature"/>); null on anything the assistant didn't write.</summary>
    public string? Signature { get; set; }
}
