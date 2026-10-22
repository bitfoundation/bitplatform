using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MessageActions;

public partial class BitAiMessageActionsDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "CanRegenerate",
            Type = "bool",
            DefaultValue = "true",
            Description = "Offers regeneration of the message, when a Session is given. Only the newest assistant message should offer it, since regenerating an older one discards everything after it.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiMessageActionsClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the message actions.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ExtraActionsTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The extra buttons appended to the row of actions, after the feedback buttons. They are revealed and hidden with the built-in ones.",
        },
        new()
        {
            Name = "Message",
            Type = "BitAiMessage",
            DefaultValue = "",
            Description = "The message these actions belong to (required): what copy copies, what feedback is recorded on, and where the model, the usage and the time are read from.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chatmessage#message",
        },
        new()
        {
            Name = "OnFeedback",
            Type = "EventCallback<BitAiFeedbackKind>",
            DefaultValue = "",
            Description = "The callback for when the user records feedback, so a host can persist it: Positive or Negative, or None when the user pressed the chosen one again to withdraw it. The message's Feedback is already updated when it is raised.",
            LinkType = LinkType.Link,
            Href = "#feedback-kind-enum",
        },
        new()
        {
            Name = "Pinned",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the actions visible rather than revealing them while the message is hovered or holds the focus.",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session driving the conversation of the message, which enables regeneration.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "ShowFeedback",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the thumbs up and thumbs down buttons.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiMessageActionsClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the message actions.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiMessageActions-gap", DefaultValue = "spacing(1.5)", Description = "Room between the group of actions and each piece of metadata." },
        new() { Name = "--bit-AiMessageActions-actions-gap", DefaultValue = "spacing(0.5)", Description = "Room between two action buttons." },
        new() { Name = "--bit-AiMessageActions-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Text of the metadata: the model, the usage and the time." },
        new() { Name = "--bit-AiMessageActions-font-size", DefaultValue = "var(--bit-tpg-fs-xs)", Description = "Size of the metadata." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiMessageActionsClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiMessageActions."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the group of action buttons of the BitAiMessageActions."
                },
                new()
                {
                    Name = "RegenerateButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the regenerate button of the BitAiMessageActions.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "FeedbackButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the two feedback buttons of the BitAiMessageActions.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "Model",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the model name of the BitAiMessageActions."
                },
                new()
                {
                    Name = "Time",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time of the BitAiMessageActions."
                }
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "feedback-kind-enum",
            Name = "BitAiFeedbackKind",
            Description = "The sentiment a user attached to a message (BitAiMessage.Feedback).",
            Items =
            [
                new() { Name = "None", Description = "No feedback given, or the feedback was withdrawn.", Value = "0" },
                new() { Name = "Positive", Description = "The response was helpful (thumbs up).", Value = "1" },
                new() { Name = "Negative", Description = "The response was not helpful (thumbs down).", Value = "2" },
            ]
        },
    ];



    private static BitAiMessage CreateReply(string text)
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart(text));
        message.MergeMetadata(new BitAiGenerationMetadata
        {
            ModelId = AiDemoCatalog.DefaultModelId,
            Usage = new BitAiTokenUsage { InputTokens = 42, OutputTokens = 96 },
        });

        return message;
    }


    private readonly BitAiMessage message = CreateReply("A finished reply. Hover this row to reveal its actions.");


    private readonly BitAiMessage pinnedMessage = CreateReply("A reply whose actions are always shown.");


    private BitAiFeedbackKind lastFeedback;
    private readonly BitAiMessage feedbackMessage = CreateReply("Was this answer helpful?");
    private readonly BitAiMessage noFeedbackMessage = CreateReply("A reply that does not ask for feedback.");

    private void HandleFeedback(BitAiFeedbackKind feedback)
    {
        lastFeedback = feedback;
    }


    private bool canRegenerate = true;
    private readonly BitAiChatSession regenerateSession = new(AiDemoTransports.Echo(), CreateRegenerateConversation());

    private static BitAiConversation CreateRegenerateConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.User, "Say something.");
        conversation.AddMessage(CreateReply("Press regenerate and this reply is written again."));

        return conversation;
    }

    // A regenerate replaces the reply: the new one is added, streams, and the turn completes.
    private void OnRegenerateMessageAdded(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

    private void OnRegenerateTurnCompleted(BitAiMessage message) => _ = InvokeAsync(StateHasChanged);


    private bool isBookmarked;
    private readonly BitAiMessage extraMessage = CreateReply("A reply with a bookmark button of the app's own.");

    private void ToggleBookmark()
    {
        isBookmarked = isBookmarked is false;
    }


    private readonly BitAiUiOptions allMetadataOptions = new() { ShowModelName = true, ShowUsage = true, ShowTimestamps = true };
    private readonly BitAiUiOptions metadataOnlyOptions = new() { ShowModelName = true, ShowUsage = true, ShowMessageActions = false };
    private readonly BitAiMessage optionsMessage = CreateReply("The model, the usage and the time.");
    private readonly BitAiMessage metadataOnlyMessage = CreateReply("The metadata alone, without the actions.");


    private bool isDisabled = true;
    private readonly BitAiMessage disabledMessage = CreateReply("A reply whose actions are disabled.");


    private readonly BitAiMessageActionsParams[] actionsParams = [new() { Pinned = true, ShowFeedback = false }];
    private readonly BitAiMessage cascadeMessage = CreateReply("Pinned, without feedback, from the BitParams around it.");
    private readonly BitAiMessage cascadeOwnMessage = CreateReply("Pinned from the BitParams, with its own ShowFeedback.");


    private readonly BitAiMessage styleMessage = CreateReply("Styled.");
    private readonly BitAiMessage classMessage = CreateReply("Classy.");
    private readonly BitAiMessage variablesMessage = CreateReply("Restyled by its variables.");


    private readonly BitAiMessage rtlMessage = CreateReply("پاسخی که دکمه‌هایش همیشه نمایش داده می‌شوند.");



    protected override Task OnInitAsync()
    {
        regenerateSession.Conversation.MessageAdded += OnRegenerateMessageAdded;
        regenerateSession.TurnCompleted += OnRegenerateTurnCompleted;

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        regenerateSession.Conversation.MessageAdded -= OnRegenerateMessageAdded;
        regenerateSession.TurnCompleted -= OnRegenerateTurnCompleted;

        await regenerateSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
