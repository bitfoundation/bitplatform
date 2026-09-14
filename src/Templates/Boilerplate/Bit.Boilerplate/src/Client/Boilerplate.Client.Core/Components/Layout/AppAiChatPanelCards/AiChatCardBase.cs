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
