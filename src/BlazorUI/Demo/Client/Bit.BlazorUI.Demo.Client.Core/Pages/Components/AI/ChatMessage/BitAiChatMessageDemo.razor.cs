using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ChatMessage;

public partial class BitAiChatMessageDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Animate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Plays the entrance animation of the message - for one that has just arrived, never for the messages restored from history. It follows the motion tokens, so it is skipped under reduced motion.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The extra content rendered at the end of an assistant message, after its parts, its error and its sources.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiChatMessageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the chat message.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation?",
            DefaultValue = "null",
            Description = "The conversation the message belongs to, which resolves the artifacts its parts refer to.",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist#conversation",
        },
        new()
        {
            Name = "IsLatest",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether this is the newest message in the transcript. Regeneration is offered only for the newest reply, because regenerating an older one discards everything after it.",
        },
        new()
        {
            Name = "Message",
            Type = "BitAiMessage",
            DefaultValue = "",
            Description = "The message to render (required). Its Role decides the layout: a user bubble, an assistant reply or a system note.",
            LinkType = LinkType.Link,
            Href = "#message",
        },
        new()
        {
            Name = "OnCitationClick",
            Type = "EventCallback<BitAiCitation>",
            DefaultValue = "",
            Description = "The callback for when the user activates a numbered citation in the message.",
            LinkType = LinkType.Link,
            Href = "/components/ai/citationmarker",
        },
        new()
        {
            Name = "OnFeedback",
            Type = "EventCallback<BitAiMessageFeedback>",
            DefaultValue = "",
            Description = "The callback for when the user records feedback on this message (or withdraws it), so a host can persist it. The message's Feedback is already updated when it is raised.",
            LinkType = LinkType.Link,
            Href = "#message-feedback",
        },
        new()
        {
            Name = "OnSourceClick",
            Type = "EventCallback<BitAiCitationSource>",
            DefaultValue = "",
            Description = "The callback for when the user activates a source card in the list of sources under the message.",
            LinkType = LinkType.Link,
            Href = "/components/ai/sourcecard",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session driving the conversation, which enables editing a user message, regenerating and retrying a reply, and deciding the approval of a tool call or the question of an agent run.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiChatMessageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the chat message.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiChatMessage-gap", DefaultValue = "spacing(1)", Description = "Room between the parts of a message: its attachments, its bubble or body, its actions." },
        new() { Name = "--bit-AiChatMessage-user-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the bubble of a user message." },
        new() { Name = "--bit-AiChatMessage-user-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text of the bubble of a user message." },
        new() { Name = "--bit-AiChatMessage-user-max-width", DefaultValue = "spacing(80), the full width on a phone", Description = "Width the bubble of a user message stops growing at." },
        new() { Name = "--bit-AiChatMessage-user-radius", DefaultValue = "var(--bit-shp-radius-lg)", Description = "Corners of the bubble of a user message, less the one it speaks from, which stays tighter." },
        new() { Name = "--bit-AiChatMessage-assistant-background", DefaultValue = "transparent", Description = "Background of the body of an assistant message." },
        new() { Name = "--bit-AiChatMessage-assistant-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text of the body of an assistant message." },
        new() { Name = "--bit-AiChatMessage-body-gap", DefaultValue = "spacing(1.5)", Description = "Room between the parts of an assistant reply." },
        new() { Name = "--bit-AiChatMessage-system-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the pill of a system message." },
        new() { Name = "--bit-AiChatMessage-system-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text of the pill of a system message." },
        new() { Name = "--bit-AiChatMessage-system-border-color", DefaultValue = "var(--bit-clr-brd-ter)", Description = "Border of the pill of a system message." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiChatMessageClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiChatMessage."
                },
                new()
                {
                    Name = "System",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the pill a system message is shown in by the BitAiChatMessage."
                },
                new()
                {
                    Name = "Attachments",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the attachments of a user message of the BitAiChatMessage."
                },
                new()
                {
                    Name = "Bubble",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the bubble a user message is shown in by the BitAiChatMessage."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions of a user message (copy and edit) of the BitAiChatMessage."
                },
                new()
                {
                    Name = "EditButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the edit button of a user message of the BitAiChatMessage.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "Editor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the editor a user message is edited in by the BitAiChatMessage."
                },
                new()
                {
                    Name = "EditorActions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the row of buttons under the editor of the BitAiChatMessage."
                },
                new()
                {
                    Name = "CancelButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the cancel button of the editor of the BitAiChatMessage.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "SaveButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the save button of the editor of the BitAiChatMessage.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of an assistant message of the BitAiChatMessage."
                },
                new()
                {
                    Name = "MessageActions",
                    Type = "BitAiMessageActionsClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiMessageActions under an assistant message of the BitAiChatMessage.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messageactions#class-styles"
                }
            ]
        },
        new()
        {
            Id = "message",
            Title = "BitAiMessage",
            Description = "A single message: a role, an ordered list of heterogeneous parts, and the metadata around them. Mutable and observable: streaming mutates it in place and raises Changed for structural changes and PartChanged for content changes inside a part, so a token re-renders one small subtree. Build one with new BitAiMessage(role, messageId?) and AddPart, or let BitAiConversation.AddMessage(role, text) do it; the parts, attachments and citations are copy-on-write snapshots, safe to read while a stream writes.",
            Parameters =
            [
                new()
                {
                    Name = "MessageId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the message, given to its constructor or generated."
                },
                new()
                {
                    Name = "Role",
                    Type = "BitAiRole",
                    DefaultValue = "",
                    Description = "Who authored it: BitAiRole.User, Assistant, System, Tool or Developer (a string value, so a provider's own role fits too).",
                    LinkType = LinkType.Link,
                    Href = "#role"
                },
                new()
                {
                    Name = "CreatedAt",
                    Type = "DateTimeOffset",
                    DefaultValue = "DateTimeOffset.UtcNow",
                    Description = "When the message was created, which a ShowTimestamps option shows under it."
                },
                new()
                {
                    Name = "Parts",
                    Type = "IReadOnlyList<BitAiMessagePart>",
                    DefaultValue = "[]",
                    Description = "The parts the message is made of, in order: BitAiTextPart, BitAiMarkdownPart, reasoning, tool calls, artifacts, images, generative UI and the rest.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messagepartview"
                },
                new()
                {
                    Name = "Attachments",
                    Type = "IReadOnlyList<BitAiAttachment>",
                    DefaultValue = "[]",
                    Description = "The files the message carries, shown as chips above a user bubble.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/attachmentchip"
                },
                new()
                {
                    Name = "Citations",
                    Type = "IReadOnlyList<BitAiCitation>",
                    DefaultValue = "[]",
                    Description = "The citations of the message, each numbered by AddCitation (a source cited twice keeps its number).",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/citationmarker"
                },
                new()
                {
                    Name = "Status",
                    Type = "BitAiMessageStatus",
                    DefaultValue = "BitAiMessageStatus.Complete",
                    Description = "Where the message is in its lifecycle. A Pending one with no part yet shows a thinking indicator.",
                    LinkType = LinkType.Link,
                    Href = "#message-status-enum"
                },
                new()
                {
                    Name = "IsBusy",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True while the message is pending or streaming: its actions are hidden and it is aria-busy."
                },
                new()
                {
                    Name = "Metadata",
                    Type = "BitAiGenerationMetadata?",
                    DefaultValue = "null",
                    Description = "What the provider reported about the generation: the model, the token usage, the finish reason, the timings. Merged with MergeMetadata.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/tokenusageview"
                },
                new()
                {
                    Name = "Error",
                    Type = "BitAiError?",
                    DefaultValue = "null",
                    Description = "The failure that ended the message, shown with a retry when it is retryable.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/errorview"
                },
                new()
                {
                    Name = "StatusText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A transient status line shown while the message is generating (\"Searching the documentation\")."
                },
                new()
                {
                    Name = "Feedback / FeedbackComment",
                    Type = "BitAiFeedbackKind / string?",
                    DefaultValue = "BitAiFeedbackKind.None / null",
                    Description = "The sentiment the user attached to the message, and the free text that came with it.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messageactions#feedback-kind-enum"
                },
                new()
                {
                    Name = "Run",
                    Type = "BitAiAgentRun?",
                    DefaultValue = "null",
                    Description = "The agent run the message reports, drawn above its parts.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentrunview"
                },
                new()
                {
                    Name = "Extensions",
                    Type = "Dictionary<string, object?>",
                    DefaultValue = "[]",
                    Description = "Application-defined values attached to the message."
                },
                new()
                {
                    Name = "AddPart(part) / InsertPart(index, part) / RemovePart(partId)",
                    Type = "TPart / TPart / bool",
                    DefaultValue = "",
                    Description = "Adds a part at the end or at a position, or removes one."
                },
                new()
                {
                    Name = "FindPart(partId) / PartsOf<TPart>() / FindToolCall(toolCallId)",
                    Type = "BitAiMessagePart? / IEnumerable<TPart> / BitAiToolCallPart?",
                    DefaultValue = "",
                    Description = "Finds a part by its identifier, enumerates the parts of a type, or finds a tool call anywhere in the message."
                },
                new()
                {
                    Name = "AddAttachment(attachment) / RemoveAttachment(attachmentId)",
                    Type = "void / bool",
                    DefaultValue = "",
                    Description = "Adds or removes a file of the message."
                },
                new()
                {
                    Name = "AddCitation(citation) / GetSources()",
                    Type = "BitAiCitation / IReadOnlyList<BitAiCitationSource>",
                    DefaultValue = "",
                    Description = "Adds a citation, deduplicated by source and given the next marker number (returned with it); GetSources lists the distinct sources cited, in marker order."
                },
                new()
                {
                    Name = "SetStatus(status) / SetStatusText(text) / SetError(error)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Moves the message to a status, sets its transient status line, or records the failure that ended it."
                },
                new()
                {
                    Name = "MergeMetadata(metadata)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Merges generation metadata, keeping the existing values wherever the incoming record leaves a field null - providers report it piecemeal."
                },
                new()
                {
                    Name = "SetFeedback(feedback, comment?)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Records the user's sentiment about the message."
                },
                new()
                {
                    Name = "GetText()",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The text of the message: its text parts joined - what the copy button copies."
                },
                new()
                {
                    Name = "Changed / PartChanged",
                    Type = "event Action<BitAiMessage>? / event Action<BitAiMessage, BitAiMessagePart>?",
                    DefaultValue = "",
                    Description = "Raised after a structural change (a part added or removed, the status moving), and after a part reports a content change."
                }
            ]
        },
        new()
        {
            Id = "role",
            Title = "BitAiRole",
            Description = "Who authored a message: a readonly record struct around a string value, compared without regard to case, so a provider's own role converts to one implicitly.",
            Parameters =
            [
                new()
                {
                    Name = "User / Assistant / System / Tool / Developer",
                    Type = "BitAiRole",
                    DefaultValue = "",
                    Description = "The well-known roles, \"user\", \"assistant\", \"system\", \"tool\" and \"developer\"."
                },
                new()
                {
                    Name = "Value",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The role as a string, which the message writes into its data-role attribute."
                },
                new()
                {
                    Name = "IsUser / IsAssistant / IsSystem / IsTool",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Which of the well-known roles it is. IsSystem is true for the developer role too: both are instructions to the model rather than turns of the conversation."
                }
            ]
        },
        new()
        {
            Id = "message-feedback",
            Title = "BitAiMessageFeedback",
            Description = "A feedback decision the user made on a message, raised by the OnFeedback of BitAiChatMessage, BitAiMessageList and BitAiChat so the host can persist it.",
            Parameters =
            [
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage",
                    DefaultValue = "",
                    Description = "The message the feedback is about. Its Feedback is already updated.",
                    LinkType = LinkType.Link,
                    Href = "#message"
                },
                new()
                {
                    Name = "Feedback",
                    Type = "BitAiFeedbackKind",
                    DefaultValue = "",
                    Description = "The new sentiment, or None when the user withdrew it.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messageactions#feedback-kind-enum"
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "message-status-enum",
            Name = "BitAiMessageStatus",
            Description = "The lifecycle of a single message.",
            Items =
            [
                new() { Name = "Draft", Description = "Composed locally and not yet handed to a transport.", Value = "0" },
                new() { Name = "Pending", Description = "Submitted; the session is waiting for the first event. A reply with no part yet shows a thinking indicator.", Value = "1" },
                new() { Name = "Streaming", Description = "Events are arriving and the message is growing.", Value = "2" },
                new() { Name = "Complete", Description = "The message finished normally.", Value = "3" },
                new() { Name = "Cancelled", Description = "The message was cancelled by the user or the host; its partial content is kept.", Value = "4" },
                new() { Name = "Failed", Description = "The message terminated with an error.", Value = "5" },
            ]
        },
    ];



    private static BitAiMessage CreateMessage(BitAiRole role, string text)
    {
        var message = new BitAiMessage(role);

        message.AddPart(new BitAiTextPart(text));

        return message;
    }

    private static BitAiMessage CreateReply(string text)
    {
        var message = CreateMessage(BitAiRole.Assistant, text);

        message.MergeMetadata(new BitAiGenerationMetadata
        {
            ModelId = AiDemoCatalog.DefaultModelId,
            Usage = new BitAiTokenUsage { InputTokens = 42, OutputTokens = 96 },
        });

        return message;
    }


    private readonly BitAiMessage userMessage = CreateMessage(BitAiRole.User, "Can you lay a message out by its role?");
    private readonly BitAiMessage assistantMessage = CreateReply("Yes. A reply is **full-width prose**, with copy and feedback under it - hover it to see them.");
    private readonly BitAiMessage systemMessage = CreateMessage(BitAiRole.System, "The assistant joined the conversation.");


    private readonly BitAiConversation partsConversation = AiDemoConversations.Long(8);


    private readonly BitAiChatSession session = new(AiDemoInteractiveTransports.FailsThenRecovers());

    private async Task AskSession()
    {
        await session.SubmitAsync("What is the weather like in Oslo?");
    }

    // The messages a turn adds and removes (an edit and a regenerate rewind the thread) are drawn by this page's loop;
    // what changes inside a message is the message's own to render.
    private void OnSessionMessagesChanged(BitAiConversation conversation, BitAiMessage message) => _ = InvokeAsync(StateHasChanged);

    // And the end of a turn started from inside a message (a retry, an edit, a regenerate) re-enables the Ask button.
    private void OnSessionTurnCompleted(BitAiMessage message) => _ = InvokeAsync(StateHasChanged);


    private readonly List<BitAiMessage> animatedMessages = [CreateMessage(BitAiRole.User, "Add a few messages below.")];

    private void AddAnimatedMessage()
    {
        animatedMessages.Add(animatedMessages.Count % 2 == 0
            ? CreateMessage(BitAiRole.User, $"Message {animatedMessages.Count + 1}, from you.")
            : CreateReply($"Message {animatedMessages.Count + 1}, from the assistant, sliding in."));
    }


    private string? trackingStatus;
    private readonly BitAiMessage childContentMessage = CreateReply("Your order shipped this morning and should arrive on Thursday.");

    private void TrackOrder()
    {
        trackingStatus = $"Opened the tracking page of order #4521 at {DateTime.Now:t}.";
    }


    private string? lastEvent;
    private readonly BitAiMessage citedMessage = CreateCitedMessage();

    private static BitAiMessage CreateCitedMessage()
    {
        const string first = "On the server, for as long as the circuit lives.";
        const string second = " When the circuit drops, the state goes with it.";

        var reply = CreateReply(first + second);

        reply.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Blazor render modes")
        {
            Kind = BitAiSourceKind.Web,
            SiteName = "Microsoft Learn",
            Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
        })
        {
            StartIndex = 0,
            EndIndex = first.Length,
        });
        reply.AddCitation(new BitAiCitation("c2", new BitAiCitationSource("s2", "SignalR circuit lifetime")
        {
            Kind = BitAiSourceKind.Document,
            SiteName = "Microsoft Learn",
            Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
        })
        {
            StartIndex = first.Length,
            EndIndex = first.Length + second.Length,
        });

        return reply;
    }

    private void HandleCitationClick(BitAiCitation citation)
    {
        lastEvent = $"Citation [{citation.Number}] of \"{citation.Source.Title}\"";
    }

    private void HandleSourceClick(BitAiCitationSource source)
    {
        lastEvent = $"Source \"{source.Title}\" ({source.Url})";
    }

    private void HandleFeedback(BitAiMessageFeedback feedback)
    {
        lastEvent = $"Feedback {feedback.Feedback}";
    }


    private bool isDisabled = true;
    private readonly BitAiChatSession disabledSession = new(AiDemoTransports.Echo(), CreateDisabledConversation());

    private static BitAiConversation CreateDisabledConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(CreateMessage(BitAiRole.User, "Hover me: my copy and edit buttons are disabled too."));
        conversation.AddMessage(CreateReply("And so are the actions under this reply."));

        return conversation;
    }


    private readonly BitAiMessage styleUserMessage = CreateMessage(BitAiRole.User, "A bubble with a style of its own.");
    private readonly BitAiMessage styleAssistantMessage = CreateReply("A reply with a class on its root and its body.");
    private readonly BitAiMessage variablesUserMessage = CreateMessage(BitAiRole.User, "A bubble restyled by the variables of its container.");
    private readonly BitAiMessage variablesSystemMessage = CreateMessage(BitAiRole.System, "So is this note.");


    private readonly BitAiMessage rtlUserMessage = CreateMessage(BitAiRole.User, "می‌توانی پیام را بر اساس نقشش بچینی؟");
    private readonly BitAiMessage rtlAssistantMessage = CreateReply("بله. پاسخ به صورت **متن تمام‌عرض** نمایش داده می‌شود و دکمه‌های کپی و بازخورد زیر آن هستند.");



    protected override Task OnInitAsync()
    {
        session.Conversation.MessageAdded += OnSessionMessagesChanged;
        session.Conversation.MessageRemoved += OnSessionMessagesChanged;
        session.TurnCompleted += OnSessionTurnCompleted;

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        session.Conversation.MessageAdded -= OnSessionMessagesChanged;
        session.Conversation.MessageRemoved -= OnSessionMessagesChanged;
        session.TurnCompleted -= OnSessionTurnCompleted;

        await session.DisposeAsync();
        await disabledSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
