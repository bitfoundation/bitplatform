namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MessageList;

public partial class BitAiMessageListDemo
{
    private readonly string example1RazorCode = @"
<div style=""height:360px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""conversation"" />
</div>";
    private readonly string example1CsharpCode = @"
private readonly BitAiConversation conversation = CreateConversation();

private static BitAiConversation CreateConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""What is BitAiMessageList for?"");
    conversation.AddMessage(BitAiRole.Assistant, ""It renders the **transcript** of a conversation: every message, laid out by role, in a column that scrolls."");
    conversation.AddMessage(BitAiRole.User, ""Does it follow a streaming reply?"");
    conversation.AddMessage(BitAiRole.Assistant, ""Only while you are at the bottom. Scroll up to re-read and it leaves you where you are."");

    return conversation;
}";

    private readonly string example2RazorCode = @"
@implements IAsyncDisposable

<div style=""height:420px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""session.Conversation"" Session=""session"" />
    <BitAiPromptInput Session=""session"" />
</div>";
    private readonly string example2CsharpCode = @"
// A transport that echoes the prompt, over a conversation that already has some history.
private readonly BitAiChatSession session = new(AiDemoTransports.Echo(), CreateConversation());

private static BitAiConversation CreateConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""What is BitAiMessageList for?"");
    conversation.AddMessage(BitAiRole.Assistant, ""It renders the **transcript** of a conversation: every message, laid out by role, in a column that scrolls."");
    conversation.AddMessage(BitAiRole.User, ""Does it follow a streaming reply?"");
    conversation.AddMessage(BitAiRole.Assistant, ""Only while you are at the bottom. Scroll up to re-read and it leaves you where you are."");

    return conversation;
}

public ValueTask DisposeAsync() => session.DisposeAsync();";

    private readonly string example3RazorCode = @"
@implements IAsyncDisposable

<div style=""height:420px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""templatesSession.Conversation"" Session=""templatesSession"">
        <HeaderTemplate>
            <div style=""text-align:center;color:var(--bit-clr-fg-ter)"">Today</div>
        </HeaderTemplate>
        <EmptyTemplate>
            <BitAiEmptyState Title=""Nothing here yet""
                             Suggestions=""suggestions""
                             OnSuggestion=""AskTemplatesSession"" />
        </EmptyTemplate>
        <FooterTemplate>
            <div style=""text-align:center;color:var(--bit-clr-fg-ter)"">Replies are scripted on this page.</div>
        </FooterTemplate>
    </BitAiMessageList>
    <BitAiPromptInput Session=""templatesSession"" />
</div>";
    private readonly string example3CsharpCode = @"
private readonly BitAiChatSession templatesSession = new(AiDemoTransports.Echo());
private readonly string[] suggestions = [""What can these components do?"", ""Show me streaming""];

private async Task AskTemplatesSession(string suggestion)
{
    await templatesSession.SubmitAsync(suggestion);
}

public ValueTask DisposeAsync() => templatesSession.DisposeAsync();";

    private readonly string example4RazorCode = @"
<div style=""height:240px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""templateConversation"">
        <MessageTemplate Context=""message"">
            <div style=""display:flex;gap:0.5rem"">
                <b style=""min-width:5rem"">@(message.Role.IsUser ? ""You"" : ""Assistant"")</b>
                <span>@message.GetText()</span>
            </div>
        </MessageTemplate>
    </BitAiMessageList>
</div>";
    private readonly string example4CsharpCode = @"
// The template prints the text of a message as it is, so its conversation is written without Markdown.
private readonly BitAiConversation templateConversation = CreateTemplateConversation();

private static BitAiConversation CreateTemplateConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""What is BitAiMessageList for?"");
    conversation.AddMessage(BitAiRole.Assistant, ""It renders the transcript of a conversation: every message, laid out by role, in a column that scrolls."");
    conversation.AddMessage(BitAiRole.User, ""Can I draw the messages myself?"");
    conversation.AddMessage(BitAiRole.Assistant, ""Yes: a MessageTemplate replaces how every message is drawn, like this compact log."");

    return conversation;
}";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""hideSystemMessages"" Text=""HideSystemMessages"" />

<div style=""height:300px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""systemConversation"" HideSystemMessages=""hideSystemMessages"" />
</div>";
    private readonly string example5CsharpCode = @"
private bool hideSystemMessages = true;
private readonly BitAiConversation systemConversation = CreateSystemConversation();

private static BitAiConversation CreateSystemConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.System, ""You are a concise assistant. Answer in one sentence."");
    conversation.AddMessage(BitAiRole.User, ""What is a transcript?"");
    conversation.AddMessage(BitAiRole.Assistant, ""The written record of a conversation, in the order it was said."");

    return conversation;
}";

    private readonly string example6RazorCode = @"
@implements IAsyncDisposable

<BitToggle @bind-Value=""autoScroll"" Text=""AutoScroll"" />

<div style=""height:440px;display:flex;flex-direction:column"">
    <BitAiOptionsProvider UiOptions=""@(autoScroll ? null : manualScrollOptions)"">
        <BitAiMessageList @ref=""followList""
                          Conversation=""followSession.Conversation""
                          Session=""followSession""
                          OnPinnedChange=""HandlePinnedChange"" />
        <div style=""position:relative"">
            <BitAiScrollToLatest Visible=""@(isPinned is false)"" OnClick=""ScrollToLatest"" />
            <BitAiPromptInput Session=""followSession"" />
        </div>
    </BitAiOptionsProvider>
</div>

<div>Following the newest message: <b>@isPinned</b></div>";
    private readonly string example6CsharpCode = @"
private BitAiMessageList? followList;
private bool isPinned = true;

// Null hands the registered options down unchanged, which follow new content (AutoScroll is on by default).
private bool autoScroll = true;
private readonly BitAiUiOptions manualScrollOptions = new() { AutoScroll = false };

// A transport that streams a long Markdown answer, over a conversation that already has some history.
private readonly BitAiChatSession followSession = new(AiDemoTransports.Markdown(), AiDemoConversations.Long(12));

private void HandlePinnedChange(bool pinned)
{
    isPinned = pinned;
}

private async Task ScrollToLatest()
{
    if (followList is not null) await followList.ScrollToLatestAsync();
}

public ValueTask DisposeAsync() => followSession.DisposeAsync();";

    private readonly string example7RazorCode = @"
<div style=""height:440px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""longConversation"" />
</div>";
    private readonly string example7CsharpCode = @"
// Four hundred messages of prose, code, tables, tool calls and citations.
private readonly BitAiConversation longConversation = AiDemoConversations.Long(400);";

    private readonly string example8RazorCode = @"
<div style=""height:400px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""citedConversation""
                      OnCitationClick=""HandleCitationClick""
                      OnSourceClick=""HandleSourceClick""
                      OnFeedback=""HandleFeedback"" />
</div>

<div>Last event: <b>@(lastEvent ?? ""-"")</b></div>";
    private readonly string example8CsharpCode = @"
private string? lastEvent;
private readonly BitAiConversation citedConversation = CreateCitedConversation();

private static BitAiConversation CreateCitedConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""Where does an Interactive Server component keep its state?"");

    const string first = ""On the server, for as long as the circuit lives."";
    const string second = "" When the circuit drops, the state goes with it."";

    var reply = new BitAiMessage(BitAiRole.Assistant);
    reply.AddPart(new BitAiTextPart(first + second));

    // Each citation is anchored to the span of the text it supports: its numbered chip goes at the end of it.
    reply.AddCitation(new BitAiCitation(""c1"", new BitAiCitationSource(""s1"", ""Blazor render modes"")
    {
        Kind = BitAiSourceKind.Web,
        SiteName = ""Microsoft Learn"",
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/components/render-modes"",
    })
    {
        StartIndex = 0,
        EndIndex = first.Length,
    });
    reply.AddCitation(new BitAiCitation(""c2"", new BitAiCitationSource(""s2"", ""SignalR circuit lifetime"")
    {
        Kind = BitAiSourceKind.Document,
        SiteName = ""Microsoft Learn"",
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr"",
    })
    {
        StartIndex = first.Length,
        EndIndex = first.Length + second.Length,
    });

    conversation.AddMessage(reply);

    return conversation;
}

private void HandleCitationClick(BitAiCitation citation)
{
    lastEvent = $""Citation [{citation.Number}] of \""{citation.Source.Title}\"""";
}

private void HandleSourceClick(BitAiCitationSource source)
{
    lastEvent = $""Source \""{source.Title}\"" ({source.Url})"";
}

private void HandleFeedback(BitAiMessageFeedback feedback)
{
    lastEvent = $""Feedback {feedback.Feedback} on message {feedback.Message.MessageId}"";
}";

    private readonly string example9RazorCode = @"
@implements IAsyncDisposable

<BitToggle @bind-Value=""isDisabled"" Text=""Disabled"" />

<div style=""height:360px;display:flex;flex-direction:column"">
    <BitAiMessageList Disabled=""isDisabled"" Conversation=""disabledSession.Conversation"" Session=""disabledSession"" />
</div>";
    private readonly string example9CsharpCode = @"
private bool isDisabled = true;
private readonly BitAiChatSession disabledSession = new(AiDemoTransports.Echo(), CreateConversation());

private static BitAiConversation CreateConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""What is BitAiMessageList for?"");
    conversation.AddMessage(BitAiRole.Assistant, ""It renders the **transcript** of a conversation: every message, laid out by role, in a column that scrolls."");
    conversation.AddMessage(BitAiRole.User, ""Does it follow a streaming reply?"");
    conversation.AddMessage(BitAiRole.Assistant, ""Only while you are at the bottom. Scroll up to re-read and it leaves you where you are."");

    return conversation;
}

public ValueTask DisposeAsync() => disabledSession.DisposeAsync();";

    private readonly string example10RazorCode = @"
<BitParams Parameters=""listParams"">
    <div style=""height:260px;display:flex;flex-direction:column"">
        <BitAiMessageList Conversation=""cascadeConversation"" />
    </div>

    <div style=""height:260px;display:flex;flex-direction:column"">
        <BitAiMessageList Conversation=""cascadeOwnConversation"" HideSystemMessages />
    </div>
</BitParams>";
    private readonly string example10CsharpCode = @"
private readonly BitAiMessageListParams[] listParams =
[
    new()
    {
        HideSystemMessages = false,
        Styles = new() { Root = ""border:1px dashed var(--bit-clr-brd-pri);border-radius:0.5rem"" },
    }
];
private readonly BitAiConversation cascadeConversation = CreateSystemConversation();
private readonly BitAiConversation cascadeOwnConversation = CreateSystemConversation();

private static BitAiConversation CreateSystemConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.System, ""You are a concise assistant. Answer in one sentence."");
    conversation.AddMessage(BitAiRole.User, ""What is a transcript?"");
    conversation.AddMessage(BitAiRole.Assistant, ""The written record of a conversation, in the order it was said."");

    return conversation;
}";

    private readonly string example11RazorCode = @"
<style>
    .custom-list {
        border-radius: 1rem;
        background: linear-gradient(180deg, var(--bit-clr-pri-tint), transparent 60%);
    }

    .custom-list-content {
        gap: 1.5rem;
        padding: 1.5rem;
    }
</style>

<div style=""height:300px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""styleConversation""
                      Style=""border:1px solid var(--bit-clr-brd-pri);border-radius:1rem""
                      Styles=""@(new() { Content = ""padding:1rem 2rem"" })"" />
</div>

<div style=""height:300px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""classConversation""
                      Class=""custom-list""
                      Classes=""@(new() { Content = ""custom-list-content"" })"" />
</div>

<div style=""height:300px;display:flex;flex-direction:column"">
    <BitAiMessageList Conversation=""variablesConversation""
                      Style=""--bit-AiMessageList-max-width:30rem;--bit-AiMessageList-gap:0.75rem;--bit-AiMessageList-padding:1rem;--bit-AiMessageList-scrollbar-color:var(--bit-clr-pri)"" />
</div>";
    private readonly string example11CsharpCode = @"
private readonly BitAiConversation styleConversation = CreateConversation();
private readonly BitAiConversation classConversation = CreateConversation();
private readonly BitAiConversation variablesConversation = CreateConversation();

private static BitAiConversation CreateConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""What is BitAiMessageList for?"");
    conversation.AddMessage(BitAiRole.Assistant, ""It renders the **transcript** of a conversation: every message, laid out by role, in a column that scrolls."");
    conversation.AddMessage(BitAiRole.User, ""Does it follow a streaming reply?"");
    conversation.AddMessage(BitAiRole.Assistant, ""Only while you are at the bottom. Scroll up to re-read and it leaves you where you are."");

    return conversation;
}";

    private readonly string example12RazorCode = @"
<div style=""height:300px;display:flex;flex-direction:column"">
    <BitAiMessageList Dir=""BitDir.Rtl"" Conversation=""rtlConversation"" />
</div>";
    private readonly string example12CsharpCode = @"
private readonly BitAiConversation rtlConversation = CreateRtlConversation();

private static BitAiConversation CreateRtlConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""این فهرست پیام‌ها برای چیست؟"");
    conversation.AddMessage(BitAiRole.Assistant, ""متن کامل یک گفتگو را نشان می‌دهد: هر پیام به ترتیب، با چیدمان نقش خودش."");

    return conversation;
}";
}
