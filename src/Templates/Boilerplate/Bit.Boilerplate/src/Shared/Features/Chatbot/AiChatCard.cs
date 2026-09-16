namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// A component a tool shows in the conversation (See <c>AppChatbot.ShowCard</c>). It is stored on the device with the
/// messages, and resent as history through <see cref="RawMarkdown"/>.
/// </summary>
public class AiChatCard : AiChatItem
{
    /// <summary>Namespace + class of a component deriving from <c>AiChatCardBase</c> (See <see cref="AiChatCardComponents"/>).</summary>
    public string? ComponentType { get; set; }

    /// <summary>What the card renders, keyed the way that card reads it.</summary>
    public Dictionary<string, string?> Data { get; set; } = [];

    /// <summary>What the card showed, as markdown: resent to the model like an answer, and rendered where this app version has no component for it.</summary>
    public string? RawMarkdown { get; set; }

    /// <summary>The server's signature over <see cref="RawMarkdown"/> (See <see cref="AiChatMessage.Signature"/>); null in a voice call.</summary>
    public string? Signature { get; set; }
}
