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
            Description = "Offers regeneration of the message, when a Session is given. Only the newest assistant message should offer it, since regenerating an older one discards everything after it; a BitAiChatMessage sets it from its IsLatest.",
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
            Description = "The extra buttons appended to the row of actions, after the feedback buttons. They are revealed and hidden with the built-in ones, and left out with them when the ShowMessageActions of the options in force is off.",
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
            Description = "The callback for when the user records feedback, so a host can persist it: Positive or Negative, or None when the user pressed the chosen one again to withdraw it. The message's Feedback is already updated when it is raised. A BitAiChatMessage (and the BitAiMessageList and BitAiChat around it) reports the same choice through its own OnFeedback as a BitAiMessageFeedback, which pairs it with the message (see the BitAiChatMessage page).",
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
            Name = "ReadAloud",
            Type = "BitAiReadAloud?",
            DefaultValue = "null",
            Description = "What reads the message aloud, which offers the \"Read aloud\" action. When null, the one cascaded by an ancestor - a BitAiChat with AllowReadAloud on - is used, and with none the action is not offered.",
            LinkType = LinkType.Link,
            Href = "#read-aloud",
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
        new() { Name = "--bit-AiMessageActions-error-color", DefaultValue = "var(--bit-clr-err-fg)", Description = "Text saying that reading the reply aloud failed." },
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
                    Name = "ReadAloudButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the read aloud button of the BitAiMessageActions.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "ReadAloudStatus",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the status of the BitAiMessageActions saying that reading the message aloud failed."
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
        new()
        {
            Id = "read-aloud",
            Title = "BitAiReadAloud",
            Description = "Reads replies aloud through the app's own text-to-speech: made with an IBitAiTextToSpeech, or a synthesizer delegate (the reply and a token in, a BitAiSpeechAudio - FromStream, FromBytes or FromUrl - out), it plays the speech in the browser. One instance drives the action of every reply of a chat; a BitAiChat with AllowReadAloud makes one and cascades it. One reading plays at a time on a page, nothing is read while a dictation records, and a dictation starting silences what plays.",
            Parameters =
            [
                new()
                {
                    Name = "KeepReading",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether a reading goes on with each reply that finishes after the one it was started on (ReadNextAsync), until it is stopped. Read as a reading starts."
                },
                new()
                {
                    Name = "Language",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The language handed to the IBitAiTextToSpeech service, as a BCP 47 language tag; null leaves it to the service."
                },
                new()
                {
                    Name = "PlaybackRate",
                    Type = "double",
                    DefaultValue = "1",
                    Description = "The speed the speech plays at, 1 being as synthesized; the browser keeps its pitch."
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage?",
                    DefaultValue = "null",
                    Description = "The reply being read - loading, playing, waiting to finish arriving, or, while keep reading follows the conversation, the last one it read."
                },
                new()
                {
                    Name = "IsLoading",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the speech of the reply is being synthesized."
                },
                new()
                {
                    Name = "IsPlaying",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether speech is playing."
                },
                new()
                {
                    Name = "IsFollowing",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether keep reading follows the conversation."
                },
                new()
                {
                    Name = "FailedMessage",
                    Type = "BitAiMessage?",
                    DefaultValue = "null",
                    Description = "The reply whose reading failed last, until another reading starts or the reading is stopped - for its button to say so."
                },
                new()
                {
                    Name = "Error",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "What failed in the reading of the FailedMessage: synthesis-failed or playback-failed."
                },
                new()
                {
                    Name = "IsReading(message)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether the reply is the one being read, which its button stops rather than starts."
                },
                new()
                {
                    Name = "HasFailed(message)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether the reading of the reply is the one that failed last."
                },
                new()
                {
                    Name = "PrepareAsync()",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Has the browser wake the player inside every click of an element carrying data-bit-ai-read-aloud, so the speech may play on Blazor Server, where the press reaches .NET only once the gesture is over. The actions of a reply carry the attribute and call it; a button of the app's own does the same."
                },
                new()
                {
                    Name = "ReadAsync(message)",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Reads a reply, in place of whatever is being read. A reply still arriving is read once it has finished."
                },
                new()
                {
                    Name = "ToggleAsync(message)",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Stops the reading of the reply when it is the one being read, and reads it otherwise - what the action's button does."
                },
                new()
                {
                    Name = "ReadNextAsync(reply)",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Tells the reader a reply has finished: it is read when a reading waits for it, or when keep reading follows the conversation - only a reply that finished normally, never one stopped or failed half way. BitAiChat calls it as each turn ends (BitAiChatSession.TurnCompleted); a chat assembled by hand calls it itself."
                },
                new()
                {
                    Name = "StopAsync()",
                    Type = "Task",
                    DefaultValue = "",
                    Description = "Stops the reading, keep reading with it."
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action",
                    DefaultValue = "",
                    Description = "Raised whenever what is read, or whether it is loading or playing, changes."
                },
                new()
                {
                    Name = "Failed",
                    Type = "event Action<string>",
                    DefaultValue = "",
                    Description = "Raised when a reading fails: synthesis-failed (the synthesizer threw) or playback-failed (the browser could not play the audio). The reading is stopped, and the actions of the reply show and announce the failure."
                },
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


    private BitAiReadAloud? readAloud;
    private readonly BitAiMessage readAloudMessage = CreateReply("A reply to read aloud. Press the speaker under it to hear it, and again to stop.");
    private readonly BitAiMessage readAloudOtherMessage = CreateReply("Another reply: starting its reading stops the first one.");


    private readonly BitAiMessage rtlMessage = CreateReply("پاسخی که دکمه‌هایش همیشه نمایش داده می‌شوند.");



    protected override Task OnInitAsync()
    {
        // A real app hands BitAiReadAloud its text-to-speech service; this stand-in plays a voiceless tone about as long as
        // the reply.
        readAloud = new BitAiReadAloud(JSRuntime, (reply, cancellationToken) => AiDemoSpeech.SynthesizeAsync(reply.GetText(), cancellationToken));

        regenerateSession.Conversation.MessageAdded += OnRegenerateMessageAdded;
        regenerateSession.TurnCompleted += OnRegenerateTurnCompleted;

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        regenerateSession.Conversation.MessageAdded -= OnRegenerateMessageAdded;
        regenerateSession.TurnCompleted -= OnRegenerateTurnCompleted;

        await regenerateSession.DisposeAsync();

        if (readAloud is not null) await readAloud.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
