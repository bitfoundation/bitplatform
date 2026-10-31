namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

[BlexStore(Name = "assistant", Persist = true)]
public partial class AssistantStore
{
    [BlexState] private BitAiConversationSnapshot[] _conversations = [];
    [BlexState] private string[] _deletedIds = [];
    [BlexState] private string? _currentId;
    [BlexState] private string? _modelId;
    [BlexState] private string? _prompt;
    [BlexState] private bool _isHistoryOpen;

    [BlexAction] private void OnOpen(string? conversationId) => CurrentId = conversationId;

    [BlexAction] private void OnChooseModel(string modelId) => ModelId = modelId;

    [BlexAction] private void OnWrite(string? prompt) => Prompt = prompt;

    [BlexAction] private void OnShowHistory(bool isOpen) => IsHistoryOpen = isOpen;

    [BlexAction]
    private void OnKeep(BitAiConversationSnapshot conversation)
    {
        Conversations = [conversation, .. Conversations.Where(c => c.ConversationId != conversation.ConversationId)];
    }

    [BlexAction]
    private void OnDelete(string conversationId)
    {
        Conversations = [.. Conversations.Where(c => c.ConversationId != conversationId)];
        DeletedIds = [.. DeletedIds.Where(id => id != conversationId), conversationId];

        if (CurrentId == conversationId)
        {
            CurrentId = null;
        }
    }
}
