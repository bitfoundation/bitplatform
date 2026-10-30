namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.Chat;

public partial class BitAiChatDemo
{
    private readonly string example1RazorCode = @"
<div style=""height:400px"">
    <BitAiChat />
</div>";

    private readonly string example2RazorCode = @"
@implements IAsyncDisposable

<BitButton OnClick=""AskFromCode"">Ask from code</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""FocusComposer"">Focus the composer</BitButton>

<div style=""height:440px"">
    <BitAiChat @ref=""sessionChat"" Session=""markdownSession"" />
</div>";
    private readonly string example2CsharpCode = @"
private BitAiChat? sessionChat;

// Any IBitAiTransport: this one streams a scripted Markdown answer. Inject BitAiChatSessionFactory and call
// Create() instead for a session over the registered transport, tools and approval policy.
private readonly BitAiChatSession markdownSession = new(AiDemoTransports.Markdown());

private async Task AskFromCode()
{
    await markdownSession.SubmitAsync(""Where does Blazor run my code?"");
}

private async Task FocusComposer()
{
    if (sessionChat is not null) await sessionChat.FocusComposerAsync();
}

// A session you create is yours to dispose: it cancels the turn in flight and detaches every handler.
public ValueTask DisposeAsync() => markdownSession.DisposeAsync();";

    private readonly string example3RazorCode = @"
<BitToggle @bind-Value=""hideSystemMessages"" Text=""HideSystemMessages"" />

<div style=""height:400px"">
    <BitAiChat Conversation=""history""
               SessionOptions=""historyOptions""
               HideSystemMessages=""hideSystemMessages"" />
</div>";
    private readonly string example3CsharpCode = @"
private bool hideSystemMessages = true;
private readonly BitAiConversation history = CreateHistory();
private readonly BitAiChatSessionOptions historyOptions = new() { AutoTitle = false, Timeout = TimeSpan.FromSeconds(30) };

private static BitAiConversation CreateHistory()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.System, ""You are the assistant of the bit BlazorUI docs."");
    conversation.AddMessage(BitAiRole.User, ""Which package are the AI components in?"");
    conversation.AddMessage(BitAiRole.Assistant, ""In **Bit.BlazorUI.AI**, which builds on the core Bit.BlazorUI package."");

    return conversation;
}";

    private readonly string example4RazorCode = @"
<div style=""height:400px"">
    <BitAiChat EmptyTitle=""Hi, I am the docs assistant""
               EmptyBody=""Ask about any component, or start from one of these.""
               Suggestions=""suggestions"" />
</div>";
    private readonly string example4CsharpCode = @"
private readonly string[] suggestions =
[
    ""What can these components do?"",
    ""Show me streaming"",
    ""Explain render modes"",
];";

    private readonly string example5RazorCode = @"
<div style=""height:460px"">
    <BitAiChat @ref=""templatesChat"">
        <HeaderTemplate>
            <div style=""display:flex;align-items:center;gap:0.5rem;padding:0.5rem 1rem;border-bottom:1px solid var(--bit-clr-brd-sec)"">
                <BitIcon IconName=""@BitIconName.Robot"" />
                <b>Support assistant</b>
                <BitSpacer />
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Add"" OnClick=""NewTemplatesChat"">New chat</BitButton>
            </div>
        </HeaderTemplate>
        <EmptyTemplate>
            <div style=""display:flex;flex-direction:column;align-items:center;gap:1rem;padding:2rem 1rem;text-align:center"">
                <b>What can we help you with?</b>
                <BitButton Variant=""BitVariant.Outline"" OnClick=""AskAboutRefunds"">How do refunds work?</BitButton>
            </div>
        </EmptyTemplate>
        <ListHeaderTemplate>
            <div style=""text-align:center;color:var(--bit-clr-fg-sec)"">Chats with the assistant are not saved.</div>
        </ListHeaderTemplate>
        <ListFooterTemplate>
            <div style=""text-align:center;color:var(--bit-clr-fg-ter)"">Replies are scripted on this page.</div>
        </ListFooterTemplate>
        <FooterTemplate>
            <div style=""padding-top:0.5rem;text-align:center;color:var(--bit-clr-fg-ter)"">
                The assistant can make mistakes. Check important information.
            </div>
        </FooterTemplate>
    </BitAiChat>
</div>";
    private readonly string example5CsharpCode = @"
private BitAiChat? templatesChat;

private void NewTemplatesChat()
{
    if (templatesChat is null) return;

    templatesChat.ActiveSession.Stop();
    templatesChat.ActiveConversation.Clear();
}

private async Task AskAboutRefunds()
{
    if (templatesChat is null) return;

    await templatesChat.ActiveSession.SubmitAsync(""How do refunds work?"");
}";

    private readonly string example6RazorCode = @"
<div style=""height:420px"">
    <BitAiChat @ref=""composerChat"" Placeholder=""Ask anything..."">
        <ComposerPrefixTemplate>
            <BitAiModelSelector @bind-ModelId=""modelId"" />
        </ComposerPrefixTemplate>
        <ComposerSuffixTemplate>
            <BitButton Variant=""BitVariant.Text""
                       IconName=""@BitIconName.Delete""
                       Title=""Clear the conversation""
                       AriaLabel=""Clear the conversation""
                       OnClick=""ClearComposerChat"" />
        </ComposerSuffixTemplate>
        <ComposerHintTemplate>
            Picked: <b>@modelId</b>. Enter sends, Shift+Enter starts a new line.
        </ComposerHintTemplate>
    </BitAiChat>
</div>";
    private readonly string example6CsharpCode = @"
private BitAiChat? composerChat;

// The models come from the catalog registered with ai.AddModels(...). A session of your own takes the picked one
// through its options: new BitAiChatSessionOptions { RequestOptions = new() { ModelId = modelId } }.
private string? modelId = ""demo-model-1"";

private void ClearComposerChat()
{
    if (composerChat is null) return;

    composerChat.ActiveSession.Stop();
    composerChat.ActiveConversation.Clear();
}";

    private readonly string example7RazorCode = @"
@implements IAsyncDisposable

<div style=""height:400px"">
    <BitAiChat Session=""guidedSession"" EmptyTitle=""Pick a reply to start"">
        <ComposerTemplate>
            <div style=""display:flex;flex-wrap:wrap;justify-content:center;gap:0.5rem"">
                @foreach (var reply in guidedReplies)
                {
                    <BitButton Variant=""BitVariant.Outline""
                               Disabled=""guidedSession.IsGenerating""
                               OnClick=""() => guidedSession.SubmitAsync(reply)"">
                        @reply
                    </BitButton>
                }
            </div>
        </ComposerTemplate>
    </BitAiChat>
</div>";
    private readonly string example7CsharpCode = @"
private readonly BitAiChatSession guidedSession = new(AiDemoTransports.Echo());
private readonly string[] guidedReplies = [""Track my order"", ""Change my address"", ""Talk to a person""];

public ValueTask DisposeAsync() => guidedSession.DisposeAsync();";

    private readonly string example8RazorCode = @"
@implements IAsyncDisposable

<BitToggle @bind-Value=""allowAttachments"" Text=""AllowAttachments"" />
<BitToggle @bind-Value=""allowDictation"" Text=""AllowDictation"" />
<BitToggle @bind-Value=""offerCommands"" Text=""Commands & Mentions"" />

<div style=""height:440px"">
    <BitAiChat Session=""commandsSession""
               AllowAttachments=""allowAttachments""
               MaxAttachmentSize=""1024 * 1024""
               OnAttachmentRejected=""HandleAttachmentRejected""
               AllowDictation=""allowDictation""
               Commands=""@(offerCommands ? commands : null)""
               Mentions=""@(offerCommands ? mentions : null)""
               Suggestions=""commandSuggestions"" />
</div>

<div>Last file turned away: <b>@(rejectedFile ?? ""-"")</b></div>";
    private readonly string example8CsharpCode = @"
private bool allowAttachments = true;
private bool allowDictation;
private bool offerCommands = true;
private string? rejectedFile;

// A transport that names the slash command and the @-mentions it was sent.
private readonly BitAiChatSession commandsSession = new(AiDemoInteractiveTransports.Commands());
private IReadOnlyList<BitAiSlashCommand> commands = [];
private readonly string[] commandSuggestions = [""Compare @roadmap.md with @pricing.xlsx"", ""Ask @priya about the launch date""];

private readonly IReadOnlyList<BitAiMentionItem> mentions =
[
    new(""file-roadmap"", ""roadmap.md"", ""Product roadmap, updated Monday"", BitIconName.Page),
    new(""file-pricing"", ""pricing.xlsx"", ""Pricing model, three tiers"", BitIconName.Page),
    new(""user-priya"", ""priya"", ""Priya Raman, product lead"", BitIconName.Contact),
    new(""source-analytics"", ""analytics"", ""Usage analytics, last 30 days"", BitIconName.Database),
];

protected override void OnInitialized()
{
    commands =
    [
        // A command with a Template inserts it into the composer...
        new(""summarize"", ""Summarize the conversation so far"", BitIconName.BulletedList)
        {
            Template = ""Summarize the conversation so far in three bullet points."",
        },
        // ...and one with a Handler runs it instead.
        new(""clear"", ""Start over with an empty conversation"", BitIconName.Delete)
        {
            Handler = () =>
            {
                commandsSession.Stop();
                commandsSession.Conversation.Clear();
                return Task.CompletedTask;
            },
        },
    ];
}

private void HandleAttachmentRejected(BitAiAttachmentRejection rejection)
{
    rejectedFile = $""{rejection.FileName} ({rejection.Reason})"";
}

public ValueTask DisposeAsync() => commandsSession.DisposeAsync();";

    private readonly string example9RazorCode = @"
@implements IAsyncDisposable

<div style=""height:400px"">
    <BitAiChat Session=""submitSession"" OnSubmit=""HandleSubmit"" />
</div>

<div>Last submission: <b>@(lastSubmission ?? ""-"")</b></div>";
    private readonly string example9CsharpCode = @"
private string? lastSubmission;
private readonly BitAiChatSession submitSession = new(AiDemoTransports.Echo());

private async Task HandleSubmit(BitAiPromptSubmission submission)
{
    lastSubmission = $""\""{submission.Text}\"" with {submission.Attachments.Count} attachment(s)"";

    // Upload the attachments to your own storage here, or add what the model should know - this adds the page.
    await submitSession.SubmitAsync($""[asked on the AiChat page] {submission.Text}"", submission.Attachments);
}

public ValueTask DisposeAsync() => submitSession.DisposeAsync();";

    private readonly string example10RazorCode = @"
@implements IAsyncDisposable

<div style=""height:420px"">
    <BitAiChat Session=""templateSession"" Suggestions=""suggestions"">
        <MessageTemplate Context=""message"">
            <div style=""display:flex;gap:0.75rem;align-items:flex-start;flex-direction:@(message.Role.IsUser ? ""row-reverse"" : ""row"")"">
                <BitIcon IconName=""@(message.Role.IsUser ? BitIconName.Contact : BitIconName.Robot)"" />
                <BitAiChatMessage Message=""message""
                                  Session=""templateSession""
                                  Conversation=""templateSession.Conversation""
                                  IsLatest=""@(message == templateSession.Conversation.LastMessage)""
                                  Style=""flex:1;min-width:0"" />
            </div>
        </MessageTemplate>
    </BitAiChat>
</div>";
    private readonly string example10CsharpCode = @"
private readonly BitAiChatSession templateSession = new(AiDemoTransports.Echo());

private readonly string[] suggestions =
[
    ""What can these components do?"",
    ""Show me streaming"",
    ""Explain render modes"",
];

public ValueTask DisposeAsync() => templateSession.DisposeAsync();";

    private readonly string example11RazorCode = @"
@implements IAsyncDisposable

<div style=""height:460px"">
    <BitAiChat Session=""citedSession""
               Suggestions=""citedSuggestions""
               OnCitationClick=""HandleCitationClick""
               OnSourceClick=""HandleSourceClick""
               OnFeedback=""HandleFeedback"" />
</div>

<div>Last event: <b>@(lastEvent ?? ""-"")</b></div>";
    private readonly string example11CsharpCode = @"
private string? lastEvent;

// A transport whose answer cites two sources, anchored to the sentences they support.
private readonly BitAiChatSession citedSession = new(AiDemoContentTransports.Cited());
private readonly string[] citedSuggestions = [""What happens to my state when the circuit drops?""];

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
}

public ValueTask DisposeAsync() => citedSession.DisposeAsync();";

    private readonly string example12RazorCode = @"
@implements IAsyncDisposable

<div style=""height:460px"">
    <BitAiChat Session=""reasoningSession"" UiOptions=""uiOptions"" Suggestions=""reasoningSuggestions"" />
</div>";
    private readonly string example12CsharpCode = @"
// A transport that streams its reasoning before the answer.
private readonly BitAiChatSession reasoningSession = new(AiDemoTransports.Reasoning());
private readonly BitAiUiOptions uiOptions = new() { CollapseReasoning = false, ShowTimestamps = true, ShowUsage = true };
private readonly string[] reasoningSuggestions = [""How long does a round trip to the model take?""];

public ValueTask DisposeAsync() => reasoningSession.DisposeAsync();";

    private readonly string example13RazorCode = @"
<BitToggle @bind-Value=""isDisabled"" Text=""Disabled"" />

<div style=""height:400px"">
    <BitAiChat Disabled=""isDisabled"" Conversation=""disabledHistory"" />
</div>";
    private readonly string example13CsharpCode = @"
private bool isDisabled = true;
private readonly BitAiConversation disabledHistory = CreateHistory();

private static BitAiConversation CreateHistory()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.System, ""You are the assistant of the bit BlazorUI docs."");
    conversation.AddMessage(BitAiRole.User, ""Which package are the AI components in?"");
    conversation.AddMessage(BitAiRole.Assistant, ""In **Bit.BlazorUI.AI**, which builds on the core Bit.BlazorUI package."");

    return conversation;
}";

    private readonly string example14RazorCode = @"
<BitParams Parameters=""chatParams"">
    <div style=""display:grid;gap:1rem;grid-template-columns:repeat(auto-fit, minmax(16rem, 1fr))"">
        <div style=""height:320px"">
            <BitAiChat />
        </div>
        <div style=""height:320px"">
            <BitAiChat Placeholder=""Its own placeholder wins..."" />
        </div>
    </div>
</BitParams>";
    private readonly string example14CsharpCode = @"
private readonly BitAiChatParams[] chatParams =
[
    new()
    {
        EmptyTitle = ""Ask the docs"",
        EmptyBody = ""Both chats take this heading and line from the BitParams around them."",
        Placeholder = ""Ask about a component..."",
        AllowAttachments = false,
    }
];";

    private readonly string example15RazorCode = @"
<style>
    .custom-chat {
        overflow: hidden;
        border-radius: 1rem;
        background: linear-gradient(180deg, var(--bit-clr-pri-tint), transparent 40%);
    }

    .custom-chat-footer {
        padding-block: 1rem;
        background: none;
    }

    .custom-chat-title {
        color: var(--bit-clr-pri);
        letter-spacing: 0.05em;
    }
</style>

<div style=""height:320px"">
    <BitAiChat EmptyTitle=""Styled""
               Style=""border:1px solid var(--bit-clr-brd-pri);border-radius:1rem;overflow:hidden""
               Styles=""@(new() { Footer = ""padding:0.5rem"",
                                 EmptyState = new() { Icon = ""color:tomato"", Title = ""font-style:italic"" } })"" />
</div>

<div style=""height:320px"">
    <BitAiChat EmptyTitle=""Classy""
               Class=""custom-chat""
               Classes=""@(new() { Footer = ""custom-chat-footer"",
                                  EmptyState = new() { Title = ""custom-chat-title"" } })"" />
</div>

<div style=""height:320px"">
    <BitAiChat EmptyTitle=""CSS variables""
               Style=""--bit-AiChat-background:var(--bit-clr-bg-sec);--bit-AiChat-max-width:28rem;--bit-AiMessageList-max-width:28rem;--bit-AiChat-footer-padding:0 1rem 1.5rem"" />
</div>";

    private readonly string example16RazorCode = @"
<div style=""height:400px"">
    <BitAiChat Dir=""BitDir.Rtl""
               EmptyTitle=""چطور می‌توانم کمک کنم؟""
               EmptyBody=""پیامی بنویسید یا یکی از پیشنهادها را انتخاب کنید.""
               Placeholder=""پیام خود را بنویسید...""
               Suggestions=""rtlSuggestions"" />
</div>";
    private readonly string example16CsharpCode = @"
private readonly string[] rtlSuggestions = [""این کامپوننت‌ها چه کارهایی می‌کنند؟"", ""استریم را نشانم بده""];";
}
