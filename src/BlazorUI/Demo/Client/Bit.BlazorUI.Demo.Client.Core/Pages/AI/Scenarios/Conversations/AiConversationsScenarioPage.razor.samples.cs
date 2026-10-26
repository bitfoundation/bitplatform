namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Conversations;

public partial class AiConversationsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="display: grid; grid-template-columns: 240px minmax(0, 1fr); gap: 16px; height: 560px">
    <BitAiConversationList Conversations="summaries"
                           SelectedId="@session?.Conversation.ConversationId"
                           OnSelect="SelectAsync"
                           OnDelete="DeleteAsync"
                           OnNew="NewConversationAsync" />

    @if (session is not null)
    {
        <BitAiChat @key="session.Conversation.ConversationId"
                   Session="session"
                   EmptyTitle="New conversation"
                   EmptyBody="Send a message and this thread will appear in the list."
                   Suggestions="suggestions" />
    }
</div>
""";

    private readonly string example1CsharpCode = """
// One live session per thread, so a reply keeps streaming while another thread is open.
private readonly Dictionary<string, BitAiChatSession> sessions = new(StringComparer.Ordinal);
private IReadOnlyList<BitAiConversationSummary> summaries = [];
private BitAiChatSession? session;

private readonly string[] suggestions = ["Plan a weekend in Lisbon", "Draft a release note"];

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;
[Inject] private IBitAiConversationStore Store { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    summaries = await Store.ListAsync();

    if (summaries.Count > 0 && await Store.LoadAsync(summaries[0].ConversationId) is BitAiConversation latest)
    {
        Open(latest);
    }
    else
    {
        Open(new BitAiConversation());
    }
}

private Task NewConversationAsync()
{
    // Reuse an untouched thread rather than stacking empty ones.
    if (session?.Conversation.Messages.Count == 0) return Task.CompletedTask;

    Open(new BitAiConversation());
    return Task.CompletedTask;
}

private async Task SelectAsync(BitAiConversationSummary summary)
{
    if (summary.ConversationId == session?.Conversation.ConversationId) return;

    if (await Store.LoadAsync(summary.ConversationId) is BitAiConversation conversation)
    {
        Open(conversation);
    }
}

private async Task DeleteAsync(BitAiConversationSummary summary)
{
    await Store.DeleteAsync(summary.ConversationId);

    if (sessions.Remove(summary.ConversationId, out var removed))
    {
        Detach(removed);
        await removed.DisposeAsync();
    }

    summaries = await Store.ListAsync();

    if (session?.Conversation.ConversationId != summary.ConversationId) return;

    session = null;

    if (summaries.Count > 0 && await Store.LoadAsync(summaries[0].ConversationId) is BitAiConversation next)
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
    if (sessions.TryGetValue(conversation.ConversationId, out var existing) is false)
    {
        existing = SessionFactory.Create(conversation);
        conversation.MessageAdded += OnMessageAdded;
        conversation.Changed += OnChanged;
        existing.TurnCompleted += OnTurnCompleted;
        sessions[conversation.ConversationId] = existing;
    }

    session = existing;
}

private void Detach(BitAiChatSession closing)
{
    closing.Conversation.MessageAdded -= OnMessageAdded;
    closing.Conversation.Changed -= OnChanged;
    closing.TurnCompleted -= OnTurnCompleted;
}

// A thread is saved when a message arrives, so an untouched one never reaches the list.
private void OnMessageAdded(BitAiConversation conversation, BitAiMessage message) =>
    _ = InvokeAsync(() => SaveAsync(conversation));

// The reply is still empty when it arrives, so the thread is saved again once its turn ends: a store over a database
// keeps what it was handed, not the live conversation, and the list's preview and order come from what it keeps.
private void OnTurnCompleted(BitAiMessage reply) =>
    _ = InvokeAsync(async () =>
    {
        // The thread is looked up among the live ones: it need not be the open one, and a deleted one is not saved again.
        var owner = sessions.Values.FirstOrDefault(s => s.Conversation.FindMessage(reply.MessageId) is not null);
        if (owner is null) return;

        await SaveAsync(owner.Conversation);
    });

private async Task SaveAsync(BitAiConversation conversation)
{
    await Store.SaveAsync(conversation);
    summaries = await Store.ListAsync();
    StateHasChanged();
}

private void OnChanged(BitAiConversation conversation) =>
    _ = InvokeAsync(async () =>
    {
        if (conversation.Messages.Count == 0) return;

        summaries = await Store.ListAsync();
        StateHasChanged();
    });

public async ValueTask DisposeAsync()
{
    foreach (var open in sessions.Values)
    {
        Detach(open);
        await open.DisposeAsync();
    }

    sessions.Clear();
}
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // Scoped, so on Blazor Server each visitor gets their own threads rather than everyone's.
    // A real app implements IBitAiConversationStore over its own database.
    ai.UseConversationStore<BitAiInMemoryConversationStore>();
});
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
    ];
}
