namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ChatMessage;

public partial class BitAiChatMessageDemo
{
    private readonly string example1RazorCode = @"
<BitAiChatMessage Message=""userMessage"" />
<BitAiChatMessage Message=""assistantMessage"" />
<BitAiChatMessage Message=""systemMessage"" />";
    private readonly string example1CsharpCode = @"
private readonly BitAiMessage userMessage = CreateMessage(BitAiRole.User, ""Can you lay a message out by its role?"");
private readonly BitAiMessage assistantMessage = CreateReply(""Yes. A reply is **full-width prose**, with copy and feedback under it - hover it to see them."");
private readonly BitAiMessage systemMessage = CreateMessage(BitAiRole.System, ""The assistant joined the conversation."");

private static BitAiMessage CreateMessage(BitAiRole role, string text)
{
    var message = new BitAiMessage(role);

    message.AddPart(new BitAiTextPart(text));

    return message;
}

// What a provider reports about a reply: the model and the token usage, shown under it.
private static BitAiMessage CreateReply(string text)
{
    var message = CreateMessage(BitAiRole.Assistant, text);

    message.MergeMetadata(new BitAiGenerationMetadata
    {
        ModelId = ""demo-model-1"",
        Usage = new BitAiTokenUsage { InputTokens = 42, OutputTokens = 96 },
    });

    return message;
}";

    private readonly string example2RazorCode = @"
@foreach (var message in partsConversation.Messages.Where(m => m.Role.IsAssistant))
{
    <BitAiChatMessage @key=""message.MessageId"" Message=""message"" Conversation=""partsConversation"" />
}";
    private readonly string example2CsharpCode = @"
// A conversation of varied replies: text, Markdown with code, a tool call with its result, a table with a citation.
private readonly BitAiConversation partsConversation = AiDemoConversations.Long(8);";

    private readonly string example3RazorCode = @"
@implements IAsyncDisposable

<BitButton OnClick=""AskSession"" Disabled=""session.IsGenerating"">Ask a question</BitButton>

@foreach (var message in session.Conversation.Messages)
{
    <BitAiChatMessage @key=""message.MessageId""
                      Message=""message""
                      Session=""session""
                      Conversation=""session.Conversation""
                      IsLatest=""@(message == session.Conversation.LastMessage)"" />
}";
    private readonly string example3CsharpCode = @"
// A transport that fails its first answer with a retryable error, and succeeds from then on.
private readonly BitAiChatSession session = new(AiDemoInteractiveTransports.FailsThenRecovers());

protected override void OnInitialized()
{
    session.Conversation.MessageAdded += OnSessionMessagesChanged;
    session.Conversation.MessageRemoved += OnSessionMessagesChanged;
    session.TurnCompleted += OnSessionTurnCompleted;
}

private async Task AskSession()
{
    await session.SubmitAsync(""What is the weather like in Oslo?"");
}

// The messages a turn adds and removes (an edit and a regenerate rewind the thread) are drawn by this loop;
// what changes inside a message is the message's own to render.
private void OnSessionMessagesChanged(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

// And the end of a turn started from inside a message (a retry, an edit, a regenerate) re-enables the Ask button.
private void OnSessionTurnCompleted(BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

public async ValueTask DisposeAsync()
{
    session.Conversation.MessageAdded -= OnSessionMessagesChanged;
    session.Conversation.MessageRemoved -= OnSessionMessagesChanged;
    session.TurnCompleted -= OnSessionTurnCompleted;

    await session.DisposeAsync();
}";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""AddAnimatedMessage"">Add a message</BitButton>

@foreach (var message in animatedMessages)
{
    <BitAiChatMessage @key=""message.MessageId"" Message=""message"" Animate />
}";
    private readonly string example4CsharpCode = @"
private readonly List<BitAiMessage> animatedMessages = [CreateMessage(BitAiRole.User, ""Add a few messages below."")];

private void AddAnimatedMessage()
{
    animatedMessages.Add(animatedMessages.Count % 2 == 0
        ? CreateMessage(BitAiRole.User, $""Message {animatedMessages.Count + 1}, from you."")
        : CreateReply($""Message {animatedMessages.Count + 1}, from the assistant, sliding in.""));
}";

    private readonly string example5RazorCode = @"
<BitAiChatMessage Message=""childContentMessage"">
    <div>
        <BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.Package"" OnClick=""TrackOrder"">Track order #4521</BitButton>
    </div>
</BitAiChatMessage>

<div>@(trackingStatus ?? ""The order has not been opened yet."")</div>";
    private readonly string example5CsharpCode = @"
private string? trackingStatus;
private readonly BitAiMessage childContentMessage = CreateReply(""Your order shipped this morning and should arrive on Thursday."");

private void TrackOrder()
{
    trackingStatus = $""Opened the tracking page of order #4521 at {DateTime.Now:t}."";
}";

    private readonly string example6RazorCode = @"
<BitAiChatMessage Message=""citedMessage""
                  OnCitationClick=""HandleCitationClick""
                  OnSourceClick=""HandleSourceClick""
                  OnFeedback=""HandleFeedback"" />

<div>Last event: <b>@(lastEvent ?? ""-"")</b></div>";
    private readonly string example6CsharpCode = @"
private string? lastEvent;
private readonly BitAiMessage citedMessage = CreateCitedMessage();

private static BitAiMessage CreateCitedMessage()
{
    const string first = ""On the server, for as long as the circuit lives."";
    const string second = "" When the circuit drops, the state goes with it."";

    var reply = CreateReply(first + second);

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

    return reply;
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
    lastEvent = $""Feedback {feedback.Feedback}"";
}";

    private readonly string example7RazorCode = @"
@implements IAsyncDisposable

<BitToggle @bind-Value=""isDisabled"" Text=""Disabled"" />

@foreach (var message in disabledSession.Conversation.Messages)
{
    <BitAiChatMessage @key=""message.MessageId""
                      Disabled=""isDisabled""
                      Message=""message""
                      Session=""disabledSession"" />
}";
    private readonly string example7CsharpCode = @"
private bool isDisabled = true;
private readonly BitAiChatSession disabledSession = new(AiDemoTransports.Echo(), CreateDisabledConversation());

private static BitAiConversation CreateDisabledConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(CreateMessage(BitAiRole.User, ""Hover me: my copy and edit buttons are disabled too.""));
    conversation.AddMessage(CreateReply(""And so are the actions under this reply.""));

    return conversation;
}

protected override void OnInitialized()
{
    disabledSession.Conversation.MessageAdded += OnSessionMessagesChanged;
    disabledSession.Conversation.MessageRemoved += OnSessionMessagesChanged;
}

// The messages a regenerate or an edit rewinds and adds (once the toggle is off) are drawn by this loop.
private void OnSessionMessagesChanged(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

public async ValueTask DisposeAsync()
{
    disabledSession.Conversation.MessageAdded -= OnSessionMessagesChanged;
    disabledSession.Conversation.MessageRemoved -= OnSessionMessagesChanged;

    await disabledSession.DisposeAsync();
}";

    private readonly string example8RazorCode = @"
<style>
    .custom-message {
        padding: 1rem;
        border-radius: 1rem;
        border: 1px dashed var(--bit-clr-pri);
    }

    .custom-message-body {
        font-style: italic;
        color: var(--bit-clr-pri);
    }
</style>

<BitAiChatMessage Message=""styleUserMessage""
                  Styles=""@(new() { Bubble = ""background:linear-gradient(135deg, #7c3aed, #2563eb);color:#fff"" })"" />

<BitAiChatMessage Message=""styleAssistantMessage""
                  Class=""custom-message""
                  Classes=""@(new() { Body = ""custom-message-body"" })"" />

<div style=""--bit-AiChatMessage-user-background:var(--bit-clr-pri);--bit-AiChatMessage-user-color:var(--bit-clr-pri-text);--bit-AiChatMessage-user-radius:0.25rem;--bit-AiChatMessage-system-background:transparent;--bit-AiChatMessage-system-border-color:var(--bit-clr-pri)"">
    <BitAiChatMessage Message=""variablesUserMessage"" />
    <BitAiChatMessage Message=""variablesSystemMessage"" />
</div>";
    private readonly string example8CsharpCode = @"
private readonly BitAiMessage styleUserMessage = CreateMessage(BitAiRole.User, ""A bubble with a style of its own."");
private readonly BitAiMessage styleAssistantMessage = CreateReply(""A reply with a class on its root and its body."");
private readonly BitAiMessage variablesUserMessage = CreateMessage(BitAiRole.User, ""A bubble restyled by the variables of its container."");
private readonly BitAiMessage variablesSystemMessage = CreateMessage(BitAiRole.System, ""So is this note."");";

    private readonly string example9RazorCode = @"
<BitAiChatMessage Dir=""BitDir.Rtl"" Message=""rtlUserMessage"" />
<BitAiChatMessage Dir=""BitDir.Rtl"" Message=""rtlAssistantMessage"" />";
    private readonly string example9CsharpCode = @"
private readonly BitAiMessage rtlUserMessage = CreateMessage(BitAiRole.User, ""می‌توانی پیام را بر اساس نقشش بچینی؟"");
private readonly BitAiMessage rtlAssistantMessage = CreateReply(""بله. پاسخ به صورت **متن تمام‌عرض** نمایش داده می‌شود و دکمه‌های کپی و بازخورد زیر آن هستند."");";
}
