namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ConversationList;

public partial class BitAiConversationListDemo
{
    private readonly string example1RazorCode = @"
<div style=""max-width:18rem"">
    <BitAiConversationList Conversations=""summaries""
                           SelectedId=""@selectedId""
                           OnSelect=""HandleSelect""
                           ShowNewButton=""false"" />
</div>

<div>Open: <b>@selectedId</b></div>";
    private readonly string example1CsharpCode = @"
private string? selectedId = ""c2"";
private readonly IReadOnlyList<BitAiConversationSummary> summaries = CreateSummaries();

private void HandleSelect(BitAiConversationSummary summary)
{
    selectedId = summary.ConversationId;
}

private static IReadOnlyList<BitAiConversationSummary> CreateSummaries()
{
    var now = DateTimeOffset.Now;

    return
    [
        new(""c1"", ""Plan a weekend in Lisbon"", now.AddMinutes(-20), ""Three days, mostly on foot, with one day trip to Sintra."", 6),
        new(""c2"", ""Draft the release notes"", now.AddHours(-2), ""Grouped by package, breaking changes first."", 4),
        new(""c3"", ""Why does my circuit drop?"", now.AddDays(-1), ""Most likely an idle timeout on the proxy."", 8),
        new(""c4"", ""Compare the pricing tiers"", now.AddDays(-4), ""The middle tier covers most teams."", 10),
        new(""c5"", ""Explain render modes"", now.AddDays(-12), ""Server, WebAssembly and Auto, per component."", 2),
        new(""c6"", ""Name the new component"", now.AddDays(-45), ""BitAiChat won the vote."", 12),
    ];
}";

    private readonly string example2RazorCode = @"
<div style=""max-width:18rem"">
    <BitAiConversationList Conversations=""editableSummaries""
                           SelectedId=""@editableSelectedId""
                           OnSelect=""HandleEditableSelect""
                           OnNew=""HandleNew""
                           OnDelete=""HandleDelete"" />
</div>";
    private readonly string example2CsharpCode = @"
private int newCount;
private string? editableSelectedId = ""c1"";
private IReadOnlyList<BitAiConversationSummary> editableSummaries = CreateSummaries();

private void HandleEditableSelect(BitAiConversationSummary summary)
{
    editableSelectedId = summary.ConversationId;
}

private void HandleNew()
{
    var summary = new BitAiConversationSummary($""new-{++newCount}"", $""New conversation {newCount}"", DateTimeOffset.Now);

    editableSummaries = [summary, .. editableSummaries];
    editableSelectedId = summary.ConversationId;
}

private void HandleDelete(BitAiConversationSummary summary)
{
    editableSummaries = editableSummaries.Where(s => s.ConversationId != summary.ConversationId).ToList();

    if (editableSelectedId == summary.ConversationId) editableSelectedId = editableSummaries.FirstOrDefault()?.ConversationId;
}";

    private readonly string example3RazorCode = @"
@implements IAsyncDisposable

<div style=""display:grid;grid-template-columns:minmax(9rem, 14rem) minmax(0, 1fr);gap:1rem;height:440px"">
    <BitAiConversationList Conversations=""chatSummaries""
                           SelectedId=""@chatSession?.Conversation.ConversationId""
                           OnSelect=""SelectChat""
                           OnNew=""NewChat""
                           OnDelete=""DeleteChat"" />
    @if (chatSession is not null)
    {
        <BitAiChat @key=""chatSession.Conversation.ConversationId""
                   Session=""chatSession""
                   EmptyTitle=""New conversation""
                   EmptyBody=""Send a message and this thread joins the list."" />
    }
</div>";
    private readonly string example3CsharpCode = @"
// One live session per thread, so a reply keeps streaming in a thread that was switched away from.
private readonly Dictionary<string, BitAiChatSession> chatSessions = new(StringComparer.Ordinal);
private IReadOnlyList<BitAiConversationSummary> chatSummaries = [];
private BitAiChatSession? chatSession;

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

protected override void OnInitialized() => OpenChat(new BitAiConversation());

private void OpenChat(BitAiConversation conversation)
{
    if (chatSessions.TryGetValue(conversation.ConversationId, out var session) is false)
    {
        session = SessionFactory.Create(conversation);
        conversation.MessageAdded += OnChatMessageAdded;
        conversation.Changed += OnChatChanged;
        chatSessions[conversation.ConversationId] = session;
    }

    chatSession = session;
}

private void NewChat()
{
    // An untouched thread is reused rather than stacking empty ones.
    if (chatSession?.Conversation.Messages.Count == 0) return;

    OpenChat(new BitAiConversation());
}

private void SelectChat(BitAiConversationSummary summary)
{
    if (chatSessions.TryGetValue(summary.ConversationId, out var session)) OpenChat(session.Conversation);
}

private async Task DeleteChat(BitAiConversationSummary summary)
{
    if (chatSessions.Remove(summary.ConversationId, out var removed) is false) return;

    removed.Conversation.MessageAdded -= OnChatMessageAdded;
    removed.Conversation.Changed -= OnChatChanged;
    await removed.DisposeAsync();

    RefreshChatSummaries();

    if (ReferenceEquals(chatSession, removed) is false) return;

    var next = chatSessions.Values.FirstOrDefault(s => s.Conversation.Messages.Count > 0);

    OpenChat(next?.Conversation ?? new BitAiConversation());
}

// Only the threads with a message in them are listed, newest first.
private void RefreshChatSummaries()
{
    chatSummaries = chatSessions.Values.Select(s => s.Conversation)
                                       .Where(c => c.Messages.Count > 0)
                                       .Select(c => c.ToSummary())
                                       .ToList();
}

// Raised from whatever thread the stream is on, so the list is refreshed on the renderer's.
private void OnChatMessageAdded(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(() =>
{
    RefreshChatSummaries();
    StateHasChanged();
});

private void OnChatChanged(BitAiConversation conversation) => _ = InvokeAsync(() =>
{
    RefreshChatSummaries();
    StateHasChanged();
});

public async ValueTask DisposeAsync()
{
    foreach (var session in chatSessions.Values)
    {
        session.Conversation.MessageAdded -= OnChatMessageAdded;
        session.Conversation.Changed -= OnChatChanged;
        await session.DisposeAsync();
    }

    chatSessions.Clear();
}";

    private readonly string example4RazorCode = @"
<div style=""max-width:18rem"">
    <BitAiConversationList Disabled
                           Conversations=""summaries""
                           SelectedId=""@selectedId""
                           OnSelect=""HandleSelect""
                           OnNew=""HandleNew""
                           OnDelete=""HandleDelete"" />
</div>";
    private readonly string example4CsharpCode = @"
// The summaries and the handlers of the Basic and the New & delete examples: none of them is raised while disabled.
private string? selectedId = ""c2"";
private readonly IReadOnlyList<BitAiConversationSummary> summaries = CreateSummaries();";

    private readonly string example5RazorCode = @"
<style>
    .custom-threads {
        border-radius: 1rem;
        border: 1px dashed var(--bit-clr-pri);
    }

    .custom-threads-item {
        border-radius: 0;
        border-inline-start: 0.25rem solid transparent;
    }

    .custom-threads-selected {
        border-inline-start-color: var(--bit-clr-pri);
    }
</style>

<BitAiConversationList Conversations=""summaries""
                       SelectedId=""@selectedId""
                       OnSelect=""HandleSelect""
                       ShowNewButton=""false""
                       Style=""border-radius:1rem;background:var(--bit-clr-bg-sec)""
                       Styles=""@(new() { GroupHeader = ""color:var(--bit-clr-pri)"", SelectedItemButton = ""font-weight:bold"" })"" />

<BitAiConversationList Conversations=""summaries""
                       SelectedId=""@selectedId""
                       OnSelect=""HandleSelect""
                       ShowNewButton=""false""
                       Class=""custom-threads""
                       Classes=""@(new() { ItemButton = ""custom-threads-item"", SelectedItemButton = ""custom-threads-selected"" })"" />

<BitAiConversationList Conversations=""summaries""
                       SelectedId=""@selectedId""
                       OnSelect=""HandleSelect""
                       ShowNewButton=""false""
                       Style=""--bit-AiConversationList-gap:0.25rem;--bit-AiConversationList-padding:0.5rem;--bit-AiConversationList-group-color:var(--bit-clr-pri);--bit-AiConversationList-item-radius:1rem;--bit-AiConversationList-item-color:var(--bit-clr-fg-sec);--bit-AiConversationList-item-background:var(--bit-clr-bg-sec);--bit-AiConversationList-item-hover-background:var(--bit-clr-bg-ter);--bit-AiConversationList-selected-color:var(--bit-clr-pri-text);--bit-AiConversationList-selected-background:var(--bit-clr-pri);--bit-AiConversationList-selected-hover-background:var(--bit-clr-pri-hover)"" />";
    private readonly string example5CsharpCode = @"
private string? selectedId = ""c2"";
private readonly IReadOnlyList<BitAiConversationSummary> summaries = CreateSummaries();

private void HandleSelect(BitAiConversationSummary summary)
{
    selectedId = summary.ConversationId;
}";

    private readonly string example6RazorCode = @"
<div dir=""rtl"" style=""max-width:18rem"">
    <BitAiConversationList Dir=""BitDir.Rtl""
                           Conversations=""rtlSummaries""
                           SelectedId=""@rtlSelectedId""
                           OnSelect=""HandleRtlSelect""
                           ShowNewButton=""false"" />
</div>";
    private readonly string example6CsharpCode = @"
private string? rtlSelectedId = ""r1"";
private readonly IReadOnlyList<BitAiConversationSummary> rtlSummaries =
[
    new(""r1"", ""برنامه‌ی سفر آخر هفته"", DateTimeOffset.Now.AddHours(-1), ""سه روز، بیشتر پیاده."", 6),
    new(""r2"", ""یادداشت‌های انتشار"", DateTimeOffset.Now.AddDays(-1), ""دسته‌بندی بر اساس بسته."", 4),
    new(""r3"", ""مقایسه‌ی طرح‌های قیمت"", DateTimeOffset.Now.AddDays(-5), ""طرح میانی برای بیشتر تیم‌ها کافی است."", 10),
];

private void HandleRtlSelect(BitAiConversationSummary summary)
{
    rtlSelectedId = summary.ConversationId;
}";
}
