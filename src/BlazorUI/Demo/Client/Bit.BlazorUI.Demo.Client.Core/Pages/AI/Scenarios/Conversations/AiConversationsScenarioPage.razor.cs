namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Conversations;

public partial class AiConversationsScenarioPage
{
    // One live session per thread, so a reply keeps streaming while another thread is open.
    private readonly Dictionary<string, BitAiChatSession> _sessions = new(StringComparer.Ordinal);
    private IReadOnlyList<BitAiConversationSummary> _summaries = [];
    private BitAiChatSession? _session;

    private readonly string[] _suggestions = ["Plan a weekend in Lisbon", "Draft a release note"];

    [AutoInject] private BitAiChatSessionFactory _sessionFactory = default!;
    [AutoInject] private IBitAiConversationStore _store = default!;

    protected override async Task OnInitAsync()
    {
        _summaries = await _store.ListAsync();

        if (_summaries.Count > 0 && await _store.LoadAsync(_summaries[0].ConversationId) is BitAiConversation latest)
        {
            Open(latest);
        }
        else
        {
            Open(new BitAiConversation());
        }

        await base.OnInitAsync();
    }

    private Task NewConversationAsync()
    {
        // Reuse an untouched thread rather than stacking empty ones.
        if (_session?.Conversation.Messages.Count == 0) return Task.CompletedTask;

        Open(new BitAiConversation());

        return Task.CompletedTask;
    }

    private async Task SelectAsync(BitAiConversationSummary summary)
    {
        if (summary.ConversationId == _session?.Conversation.ConversationId) return;

        if (await _store.LoadAsync(summary.ConversationId) is BitAiConversation conversation)
        {
            Open(conversation);
        }
    }

    private async Task DeleteAsync(BitAiConversationSummary summary)
    {
        await _store.DeleteAsync(summary.ConversationId);

        if (_sessions.Remove(summary.ConversationId, out var removed))
        {
            Detach(removed);
            await removed.DisposeAsync();
        }

        _summaries = await _store.ListAsync();

        if (_session?.Conversation.ConversationId != summary.ConversationId) return;

        _session = null;

        if (_summaries.Count > 0 && await _store.LoadAsync(_summaries[0].ConversationId) is BitAiConversation next)
        {
            Open(next);
        }
        else
        {
            Open(new BitAiConversation());
        }
    }

    private void Open(BitAiConversation conversation)
    {
        if (_sessions.TryGetValue(conversation.ConversationId, out var session) is false)
        {
            session = _sessionFactory.Create(conversation);
            conversation.MessageAdded += OnMessageAdded;
            conversation.Changed += OnChanged;
            _sessions[conversation.ConversationId] = session;
        }

        _session = session;
    }

    private void Detach(BitAiChatSession session)
    {
        session.Conversation.MessageAdded -= OnMessageAdded;
        session.Conversation.Changed -= OnChanged;
    }

    private void OnMessageAdded(BitAiConversation conversation, BitAiMessage message) =>
        _ = InvokeAsync(async () =>
        {
            await _store.SaveAsync(conversation);
            _summaries = await _store.ListAsync();
            StateHasChanged();
        });

    private void OnChanged(BitAiConversation conversation) =>
        _ = InvokeAsync(async () =>
        {
            // Only threads that have been saved belong in the list.
            if (conversation.Messages.Count == 0) return;

            _summaries = await _store.ListAsync();
            StateHasChanged();
        });

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        foreach (var session in _sessions.Values)
        {
            Detach(session);
            await session.DisposeAsync();
        }

        _sessions.Clear();

        await base.DisposeAsync(disposing);
    }
}
