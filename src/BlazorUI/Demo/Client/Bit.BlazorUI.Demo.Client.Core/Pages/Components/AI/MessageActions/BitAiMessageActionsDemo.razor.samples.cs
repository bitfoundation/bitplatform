namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MessageActions;

public partial class BitAiMessageActionsDemo
{
    private readonly string example1RazorCode = @"
<div>@message.GetText()</div>
<BitAiMessageActions Message=""message"" />";
    private readonly string example1CsharpCode = @"
private readonly BitAiMessage message = CreateReply(""A finished reply. Hover this row to reveal its actions."");

// A finished reply with what a provider reports about it: the model and the token usage.
private static BitAiMessage CreateReply(string text)
{
    var message = new BitAiMessage(BitAiRole.Assistant);

    message.AddPart(new BitAiTextPart(text));
    message.MergeMetadata(new BitAiGenerationMetadata
    {
        ModelId = ""demo-model-1"",
        Usage = new BitAiTokenUsage { InputTokens = 42, OutputTokens = 96 },
    });

    return message;
}";

    private readonly string example2RazorCode = @"
<div>@pinnedMessage.GetText()</div>
<BitAiMessageActions Pinned Message=""pinnedMessage"" />";
    private readonly string example2CsharpCode = @"
private readonly BitAiMessage pinnedMessage = CreateReply(""A reply whose actions are always shown."");";

    private readonly string example3RazorCode = @"
<div>@feedbackMessage.GetText()</div>
<BitAiMessageActions Pinned Message=""feedbackMessage"" OnFeedback=""HandleFeedback"" />

<div>Last feedback: <b>@lastFeedback</b></div>

<div>@noFeedbackMessage.GetText()</div>
<BitAiMessageActions Pinned ShowFeedback=""false"" Message=""noFeedbackMessage"" />";
    private readonly string example3CsharpCode = @"
private BitAiFeedbackKind lastFeedback;
private readonly BitAiMessage feedbackMessage = CreateReply(""Was this answer helpful?"");
private readonly BitAiMessage noFeedbackMessage = CreateReply(""A reply that does not ask for feedback."");

private void HandleFeedback(BitAiFeedbackKind feedback)
{
    lastFeedback = feedback;
}";

    private readonly string example4RazorCode = @"
@implements IAsyncDisposable

<BitToggle @bind-Value=""canRegenerate"" Text=""CanRegenerate"" />

@if (regenerateSession.Conversation.LastMessage is { } reply)
{
    @if (reply.IsBusy)
    {
        <div>Regenerating...</div>
    }
    else
    {
        <div>@reply.GetText()</div>
        <BitAiMessageActions Pinned
                             Message=""reply""
                             Session=""regenerateSession""
                             CanRegenerate=""canRegenerate"" />
    }
}";
    private readonly string example4CsharpCode = @"
private bool canRegenerate = true;

// A transport that echoes the prompt, over a conversation that already has a reply.
private readonly BitAiChatSession regenerateSession = new(AiDemoTransports.Echo(), CreateRegenerateConversation());

private static BitAiConversation CreateRegenerateConversation()
{
    var conversation = new BitAiConversation();

    conversation.AddMessage(BitAiRole.User, ""Say something."");
    conversation.AddMessage(CreateReply(""Press regenerate and this reply is written again.""));

    return conversation;
}

protected override void OnInitialized()
{
    regenerateSession.Conversation.MessageAdded += OnRegenerateMessageAdded;
    regenerateSession.TurnCompleted += OnRegenerateTurnCompleted;
}

// A regenerate replaces the reply: the new one is added, streams, and the turn completes.
private void OnRegenerateMessageAdded(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

private void OnRegenerateTurnCompleted(BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

public async ValueTask DisposeAsync()
{
    regenerateSession.Conversation.MessageAdded -= OnRegenerateMessageAdded;
    regenerateSession.TurnCompleted -= OnRegenerateTurnCompleted;

    await regenerateSession.DisposeAsync();
}";

    private readonly string example5RazorCode = @"
<div>@extraMessage.GetText()</div>
<BitAiMessageActions Pinned Message=""extraMessage"">
    <ExtraActionsTemplate>
        <BitButton Variant=""BitVariant.Text""
                   IconName=""@(isBookmarked ? BitIconName.SingleBookmarkSolid : BitIconName.SingleBookmark)""
                   Title=""@(isBookmarked ? ""Remove the bookmark"" : ""Bookmark"")""
                   AriaLabel=""@(isBookmarked ? ""Remove the bookmark"" : ""Bookmark"")""
                   aria-pressed=""@(isBookmarked ? ""true"" : ""false"")""
                   OnClick=""ToggleBookmark"" />
    </ExtraActionsTemplate>
</BitAiMessageActions>";
    private readonly string example5CsharpCode = @"
private bool isBookmarked;
private readonly BitAiMessage extraMessage = CreateReply(""A reply with a bookmark button of the app's own."");

private void ToggleBookmark()
{
    isBookmarked = isBookmarked is false;
}";

    private readonly string example6RazorCode = @"
<BitAiOptionsProvider UiOptions=""allMetadataOptions"">
    <div>@optionsMessage.GetText()</div>
    <BitAiMessageActions Pinned Message=""optionsMessage"" />
</BitAiOptionsProvider>

<BitAiOptionsProvider UiOptions=""metadataOnlyOptions"">
    <div>@metadataOnlyMessage.GetText()</div>
    <BitAiMessageActions Message=""metadataOnlyMessage"" />
</BitAiOptionsProvider>";
    private readonly string example6CsharpCode = @"
private readonly BitAiUiOptions allMetadataOptions = new() { ShowModelName = true, ShowUsage = true, ShowTimestamps = true };
private readonly BitAiUiOptions metadataOnlyOptions = new() { ShowModelName = true, ShowUsage = true, ShowMessageActions = false };
private readonly BitAiMessage optionsMessage = CreateReply(""The model, the usage and the time."");
private readonly BitAiMessage metadataOnlyMessage = CreateReply(""The metadata alone, without the actions."");";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""isDisabled"" Text=""Disabled"" />

<div>@disabledMessage.GetText()</div>
<BitAiMessageActions Pinned Disabled=""isDisabled"" Message=""disabledMessage"" />";
    private readonly string example7CsharpCode = @"
private bool isDisabled = true;
private readonly BitAiMessage disabledMessage = CreateReply(""A reply whose actions are disabled."");";

    private readonly string example8RazorCode = @"
<BitParams Parameters=""actionsParams"">
    <div>@cascadeMessage.GetText()</div>
    <BitAiMessageActions Message=""cascadeMessage"" />

    <div>@cascadeOwnMessage.GetText()</div>
    <BitAiMessageActions ShowFeedback Message=""cascadeOwnMessage"" />
</BitParams>";
    private readonly string example8CsharpCode = @"
private readonly BitAiMessageActionsParams[] actionsParams = [new() { Pinned = true, ShowFeedback = false }];
private readonly BitAiMessage cascadeMessage = CreateReply(""Pinned, without feedback, from the BitParams around it."");
private readonly BitAiMessage cascadeOwnMessage = CreateReply(""Pinned from the BitParams, with its own ShowFeedback."");";

    private readonly string example9RazorCode = @"
<style>
    .custom-actions {
        padding-block: 0.5rem;
        border-block: 1px dashed var(--bit-clr-pri);
    }

    .custom-actions-group {
        padding: 0.125rem;
        border-radius: 1rem;
        background-color: var(--bit-clr-pri-tint);
    }

    .custom-actions-model {
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }
</style>

<BitAiMessageActions Pinned
                     Message=""styleMessage""
                     Style=""padding:0.25rem 0.5rem;border-radius:1rem;background:var(--bit-clr-bg-sec)""
                     Styles=""@(new() { Model = ""font-weight:bold"", FeedbackButton = new() { Icon = ""color:tomato"" } })"" />

<BitAiMessageActions Pinned
                     Message=""classMessage""
                     Class=""custom-actions""
                     Classes=""@(new() { Actions = ""custom-actions-group"", Model = ""custom-actions-model"" })"" />

<BitAiMessageActions Pinned
                     Message=""variablesMessage""
                     Style=""--bit-AiMessageActions-gap:2rem;--bit-AiMessageActions-actions-gap:0.5rem;--bit-AiMessageActions-color:var(--bit-clr-pri);--bit-AiMessageActions-font-size:0.875rem"" />";
    private readonly string example9CsharpCode = @"
private readonly BitAiMessage styleMessage = CreateReply(""Styled."");
private readonly BitAiMessage classMessage = CreateReply(""Classy."");
private readonly BitAiMessage variablesMessage = CreateReply(""Restyled by its variables."");";

    private readonly string example10RazorCode = @"
<div dir=""rtl"">@rtlMessage.GetText()</div>
<BitAiMessageActions Pinned Dir=""BitDir.Rtl"" Message=""rtlMessage"" />";
    private readonly string example10CsharpCode = @"
private readonly BitAiMessage rtlMessage = CreateReply(""پاسخی که دکمه‌هایش همیشه نمایش داده می‌شوند."");";
}
