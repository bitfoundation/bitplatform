namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

/// <summary>
/// What the user changes on the assistant page: their own chats, the sample ones they deleted, the open chat, the
/// model, the unsent prompt and whether the history panel is open.
/// </summary>
[BlexStore(Name = "assistant", Persist = true)]
public partial class AssistantStore
{
    /// <summary>The most chats kept; older ones are dropped, so the saved state stays small.</summary>
    private const int MaxChats = 10;

    [BlexState] private AssistantChat[] _chats = [];
    [BlexState] private string[] _deletedIds = [];
    [BlexState] private string? _currentId;
    [BlexState] private string? _modelId;
    [BlexState] private string? _prompt;
    [BlexState] private bool _isHistoryOpen;

    [BlexAction] private void OnOpen(string? conversationId) => CurrentId = conversationId;

    [BlexAction] private void OnChooseModel(string modelId) => ModelId = modelId;

    [BlexAction] private void OnWrite(string? prompt) => Prompt = prompt;

    [BlexAction] private void OnShowHistory(bool isOpen) => IsHistoryOpen = isOpen;

    /// <summary>Adds the chat, or replaces its older copy, as the first one, keeping at most <c>MaxChats</c>.</summary>
    [BlexAction]
    private void OnKeep(AssistantChat chat)
    {
        Chats = [chat, .. Chats.Where(c => c.ConversationId != chat.ConversationId).Take(MaxChats - 1)];
    }

    /// <summary>Removes a kept conversation and remembers its id, so a sample one with that id stays hidden.</summary>
    [BlexAction]
    private void OnDelete(string conversationId)
    {
        Chats = [.. Chats.Where(c => c.ConversationId != conversationId)];
        DeletedIds = [.. DeletedIds.Where(id => id != conversationId), conversationId];

        if (CurrentId == conversationId)
        {
            CurrentId = null;
        }
    }
}
