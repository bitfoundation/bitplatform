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

    private readonly string example2RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    @if (session is not null)
    {
        @* Keyed by the session: the restored thread keeps its conversation id, and is still a new chat. *@
        <BitAiChat @key="session"
                   Session="session"
                   EmptyTitle="Write something to save"
                   EmptyBody="Then press Save, keep writing, and press Restore."
                   Suggestions="suggestions" />
    }
</div>

<aside aria-label="Saved JSON">
    <h4>Saved JSON</h4>
    <BitStack Horizontal Gap="0.5rem">
        <BitButton Size="BitSize.Small" OnClick="Save">Save</BitButton>
        <BitButton Size="BitSize.Small" Variant="BitVariant.Outline" Disabled="@(json is null)" OnClick="RestoreAsync">Restore</BitButton>
    </BitStack>
    @if (json is null)
    {
        <p>Nothing saved yet.</p>
    }
    else
    {
        <p>@savedCount messages, @json.Length characters:</p>
        <BitAiCodeBlockView Code="@json" Language="json" FileName="conversation.json" />
    }
</aside>
""";

    private readonly string example2CsharpCode = """
private BitAiChatSession? session;
private string? json;
private int savedCount;

private readonly string[] suggestions = ["Plan a weekend in Lisbon"];

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

protected override void OnInitialized() => session = SessionFactory.Create();

// The whole thread - messages, parts, tool calls, artifacts, agent runs - as the JSON a store would keep.
private void Save()
{
    if (session is null) return;

    json = BitAiConversationSerializer.ToJson(session.Conversation);
    savedCount = session.Conversation.Messages.Count;
}

// The JSON read back into a live conversation, and a new session to drive it; the old one only has to stop.
private async Task RestoreAsync()
{
    if (json is null) return;

    var previous = session;
    session = SessionFactory.Create(BitAiConversationSerializer.FromJson(json));

    if (previous is not null) await previous.DisposeAsync();
}

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example2StoreCode = """
// A conversation store over a folder of JSON files: the serializer writes a conversation out and reads it back.
// Registered in place of the in-memory one with ai.UseConversationStore<FileConversationStore>().
public sealed class FileConversationStore : IBitAiConversationStore
{
    private readonly string folder = Path.Combine(AppContext.BaseDirectory, "conversations");

    public async ValueTask<IReadOnlyList<BitAiConversationSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(folder) is false) return [];

        var summaries = new List<BitAiConversationSummary>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            // A snapshot is plain data: enough for the list, without building the live conversation.
            var snapshot = BitAiConversationSerializer.SnapshotFromJson(await File.ReadAllTextAsync(file, cancellationToken));
            summaries.Add(new BitAiConversationSummary(snapshot.ConversationId, snapshot.Title, snapshot.UpdatedAt,
                MessageCount: snapshot.Messages.Count));
        }

        return [.. summaries.OrderByDescending(s => s.UpdatedAt)];
    }

    public async ValueTask<IBitAiConversation?> LoadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var path = PathOf(conversationId);
        if (File.Exists(path) is false) return null;

        return BitAiConversationSerializer.FromJson(await File.ReadAllTextAsync(path, cancellationToken));
    }

    public async ValueTask SaveAsync(IBitAiConversation conversation, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(folder);

        var snapshot = BitAiConversationSerializer.ToSnapshot(conversation);
        await File.WriteAllTextAsync(PathOf(conversation.ConversationId), BitAiConversationSerializer.ToJson(snapshot), cancellationToken);
    }

    public ValueTask DeleteAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        File.Delete(PathOf(conversationId));
        return ValueTask.CompletedTask;
    }

    // The ids the package generates (conv_...) are letters, digits and an underscore, safe as file names.
    private string PathOf(string conversationId) => Path.Combine(folder, $"{conversationId}.json");
}
""";

    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("FileConversationStore.cs", example2StoreCode),
    ];
}
