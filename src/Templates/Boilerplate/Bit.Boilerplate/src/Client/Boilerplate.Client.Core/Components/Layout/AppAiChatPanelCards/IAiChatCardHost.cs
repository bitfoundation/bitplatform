using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards;

/// <summary>What a card can ask of the panel it is shown in (See AppAiChatPanel).</summary>
public interface IAiChatCardHost
{
    bool IsInVoiceCall { get; }

    /// <summary>Whether the server still waits for the user to answer <paramref name="card"/>.</summary>
    bool IsAwaited(AiChatCard card);

    /// <summary>Sends <paramref name="prompt"/> as the user's next message - into the call during one.</summary>
    Task SendPrompt(string prompt);

    /// <summary>Answers a card the server waits on (See <see cref="AiChatCardDecision"/>).</summary>
    Task Resolve(AiChatCard card, string decision);

    /// <summary>Stores <paramref name="card"/>, once the user has acted on it (See <see cref="IStoresItself"/>).</summary>
    Task Save(AiChatCard card);
}
