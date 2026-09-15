using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanelCards;

/// <summary>
/// What every card the chatbot can show in the conversation derives from. The server names one by
/// <see cref="AiChatCard.ComponentType"/> and AppAiChatPanel renders it with a DynamicComponent.
/// </summary>
public abstract partial class AiChatCardBase : AppComponentBase
{
    [Parameter, EditorRequired] public AiChatCard Message { get; set; } = default!;

    [Parameter, EditorRequired] public IAiChatCardHost Host { get; set; } = default!;
}

/// <summary>A card the panel doesn't store when it's shown: it stores itself once the user has acted on it (See IAiChatCardHost.Save).</summary>
public interface IStoresItself
{
}
