namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

[BlexStore(Name = "assistant", Persist = true)]
public partial class AssistantStore
{
    [BlexState] private AssistantConversation[] _conversations = [];
    [BlexState] private string? _currentId;
    [BlexState] private string? _prompt;
    [BlexState] private bool _isHistoryOpen;

    [BlexAction] private void OnOpen(string? conversationId) => CurrentId = conversationId;

    [BlexAction] private void OnWrite(string? prompt) => Prompt = prompt;

    [BlexAction] private void OnShowHistory(bool isOpen) => IsHistoryOpen = isOpen;

    [BlexAction]
    private void OnKeep(AssistantConversation conversation)
    {
        Conversations = [conversation, .. Conversations.Where(c => c.Id != conversation.Id)];
        CurrentId = conversation.Id;
    }
}
