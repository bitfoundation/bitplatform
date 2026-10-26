using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MessageList;

public partial class BitAiMessageListDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiMessageListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the message list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation",
            DefaultValue = "",
            Description = "The conversation to render (required). The list subscribes to it for the messages added to and removed from it; passing another one starts that one at its newest message, without animating.",
            LinkType = LinkType.Link,
            Href = "#conversation",
        },
        new()
        {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered instead of an empty list, while the conversation has no message to show.",
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered below the last message, inside the scroll area.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered above the first message, inside the scroll area. Past BitAiUiOptions.VirtualizeThreshold messages it is drawn once the reader has scrolled up to the first message, along with it.",
        },
        new()
        {
            Name = "HideSystemMessages",
            Type = "bool",
            DefaultValue = "true",
            Description = "Hides the messages the user did not write and the model did not produce (the system and developer ones). Turned off, they are shown as a centered note.",
        },
        new()
        {
            Name = "MessageTemplate",
            Type = "RenderFragment<BitAiMessage>?",
            DefaultValue = "null",
            Description = "The template that replaces the built-in rendering of a message (a BitAiChatMessage). It is keyed by the message, so removing one message never recreates the ones after it, and it adds no element of its own around what it renders. The list draws it again as the newest message changes, so a template showing the text of a reply follows it as it streams.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chatmessage#message",
        },
        new()
        {
            Name = "OnCitationClick",
            Type = "EventCallback<BitAiCitation>",
            DefaultValue = "",
            Description = "The callback for when the user activates a numbered citation in a reply.",
            LinkType = LinkType.Link,
            Href = "/components/ai/citationmarker",
        },
        new()
        {
            Name = "OnFeedback",
            Type = "EventCallback<BitAiMessageFeedback>",
            DefaultValue = "",
            Description = "The callback for when the user records feedback on a reply, so a host can persist it. The message's Feedback is already updated when it is raised.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chatmessage#message-feedback",
        },
        new()
        {
            Name = "OnPinnedChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback for when the view stops or starts following the newest message: false when the user scrolls away from it, true when they scroll back to it or ScrollToLatestAsync brings them there. It is reported once per change, not on every scroll.",
        },
        new()
        {
            Name = "OnSourceClick",
            Type = "EventCallback<BitAiCitationSource>",
            DefaultValue = "",
            Description = "The callback for when the user activates a source card in the list of sources under a reply.",
            LinkType = LinkType.Link,
            Href = "/components/ai/sourcecard",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session driving the conversation, which enables the message-level actions: editing a user message, regenerating the newest reply, retrying a failed one, deciding a tool approval.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiMessageListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the message list.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsPinned",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the view is following new content, that is the reader is at the newest message.",
        },
        new()
        {
            Name = "ScrollToLatestAsync",
            Type = "Task",
            DefaultValue = "",
            Description = "Scrolls to the newest message and resumes following new content (raising OnPinnedChange with true, when the view was not following it). Its smooth argument (true by default) animates the scroll, unless the reader asked for reduced motion.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiMessageList-max-width", DefaultValue = "spacing(92)", Description = "Width the column of messages stops growing at, centered in the list. A BitAiChat lines its composer up with it through --bit-AiChat-max-width: set both to the same value." },
        new() { Name = "--bit-AiMessageList-gap", DefaultValue = "spacing(4), spacing(3) on a phone", Description = "Room between two messages." },
        new() { Name = "--bit-AiMessageList-padding", DefaultValue = "spacing(3) spacing(2) spacing(2), spacing(2) spacing(1.5) spacing(1.5) on a phone", Description = "Inset of the column of messages." },
        new() { Name = "--bit-AiMessageList-scrollbar-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Thumb of the scrollbar of the list." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiMessageListClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element (the scroll container) of the BitAiMessageList."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content column of the BitAiMessageList, which holds the messages."
                }
            ]
        },
        new()
        {
            Id = "conversation",
            Title = "BitAiConversation",
            Description = "An ordered set of messages plus the artifacts and agent runs that belong to the whole thread - the unit of state a chat UI binds to. Create one with new BitAiConversation(conversationId?) (an id is generated when none is given), fill it from history with AddMessage, and hand it to a session. It is observable at three grains: Changed for thread-level edits, MessageAdded / MessageRemoved for the list, and the events of each message for its content. Safe for one writer and concurrent readers.",
            Parameters =
            [
                new()
                {
                    Name = "ConversationId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the conversation, given to its constructor or generated."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The display title, set with SetTitle - or by a session with AutoTitle from the first user message."
                },
                new()
                {
                    Name = "Messages",
                    Type = "IReadOnlyList<BitAiMessage>",
                    DefaultValue = "[]",
                    Description = "The messages, oldest first.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/chatmessage#message"
                },
                new()
                {
                    Name = "LastMessage",
                    Type = "BitAiMessage?",
                    DefaultValue = "null",
                    Description = "The last message, or null when the conversation is empty."
                },
                new()
                {
                    Name = "IsGenerating",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True while the last message is pending or streaming."
                },
                new()
                {
                    Name = "CreatedAt / UpdatedAt",
                    Type = "DateTimeOffset",
                    DefaultValue = "",
                    Description = "When the conversation was created, and when it last changed."
                },
                new()
                {
                    Name = "TotalUsage",
                    Type = "BitAiTokenUsage",
                    DefaultValue = "",
                    Description = "The token usage summed across every assistant message.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/tokenusageview"
                },
                new()
                {
                    Name = "Artifacts / Runs",
                    Type = "IReadOnlyDictionary<string, BitAiArtifact> / IReadOnlyDictionary<string, BitAiAgentRun>",
                    DefaultValue = "",
                    Description = "The artifacts produced and the agent runs made in this conversation, keyed by their identifiers."
                },
                new()
                {
                    Name = "Extensions",
                    Type = "Dictionary<string, object?>",
                    DefaultValue = "[]",
                    Description = "Application-defined values attached to the conversation."
                },
                new()
                {
                    Name = "AddMessage(message) / AddMessage(role, text)",
                    Type = "BitAiMessage",
                    DefaultValue = "",
                    Description = "Appends a message - one built by hand, or one built from a role and its text - and returns it."
                },
                new()
                {
                    Name = "RemoveMessage(messageId)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Removes a message; true when one was removed."
                },
                new()
                {
                    Name = "FindMessage(messageId)",
                    Type = "BitAiMessage?",
                    DefaultValue = "",
                    Description = "Finds a message by its identifier."
                },
                new()
                {
                    Name = "TruncateAfter(messageId, inclusive?)",
                    Type = "IReadOnlyList<BitAiMessage>",
                    DefaultValue = "",
                    Description = "Removes every message after the given one (and that one too when inclusive is true) and returns them, newest first - how retry and edit rewind a thread."
                },
                new()
                {
                    Name = "Clear()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Removes every message, with the artifacts and runs."
                },
                new()
                {
                    Name = "SetTitle(title)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Sets the title of the conversation."
                },
                new()
                {
                    Name = "ToSummary()",
                    Type = "BitAiConversationSummary",
                    DefaultValue = "",
                    Description = "Projects the conversation into the shape a BitAiConversationList renders.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/conversationlist#conversation-summary"
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitAiConversation>?",
                    DefaultValue = "",
                    Description = "Raised after a thread-level change, such as the title or an artifact."
                },
                new()
                {
                    Name = "MessageAdded / MessageRemoved",
                    Type = "event Action<BitAiConversation, BitAiMessage>?",
                    DefaultValue = "",
                    Description = "Raised after a message is appended or inserted, and after one is removed - from whatever thread the stream is on."
                },
                new()
                {
                    Name = "MessageUpdated",
                    Type = "event Action<BitAiConversation, BitAiMessage>?",
                    DefaultValue = "",
                    Description = "Raised when any message reports a change, structural or in one of its parts: convenient for a coarse observer such as an auto-save, while a component subscribes to the message itself."
                }
            ]
        }
    ];



    private static BitAiConversation CreateConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.User, "What is BitAiMessageList for?");
        conversation.AddMessage(BitAiRole.Assistant, "It renders the **transcript** of a conversation: every message, laid out by role, in a column that scrolls.");
        conversation.AddMessage(BitAiRole.User, "Does it follow a streaming reply?");
        conversation.AddMessage(BitAiRole.Assistant, "Only while you are at the bottom. Scroll up to re-read and it leaves you where you are.");

        return conversation;
    }


    private readonly BitAiConversation conversation = CreateConversation();


    private readonly BitAiChatSession session = new(AiDemoTransports.Echo(), CreateConversation());


    private readonly BitAiChatSession templatesSession = new(AiDemoTransports.Echo());
    private readonly string[] suggestions = ["What can these components do?", "Show me streaming"];

    private async Task AskTemplatesSession(string suggestion)
    {
        await templatesSession.SubmitAsync(suggestion);
    }


    // The template prints the text of a message as it is, so its conversation is written without Markdown.
    private readonly BitAiConversation templateConversation = CreateTemplateConversation();

    private static BitAiConversation CreateTemplateConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.User, "What is BitAiMessageList for?");
        conversation.AddMessage(BitAiRole.Assistant, "It renders the transcript of a conversation: every message, laid out by role, in a column that scrolls.");
        conversation.AddMessage(BitAiRole.User, "Can I draw the messages myself?");
        conversation.AddMessage(BitAiRole.Assistant, "Yes: a MessageTemplate replaces how every message is drawn, like this compact log.");

        return conversation;
    }


    private bool hideSystemMessages = true;
    private readonly BitAiConversation systemConversation = CreateSystemConversation();

    private static BitAiConversation CreateSystemConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.System, "You are a concise assistant. Answer in one sentence.");
        conversation.AddMessage(BitAiRole.User, "What is a transcript?");
        conversation.AddMessage(BitAiRole.Assistant, "The written record of a conversation, in the order it was said.");

        return conversation;
    }


    private BitAiMessageList? followList;
    private bool isPinned = true;
    private readonly BitAiChatSession followSession = new(AiDemoTransports.Markdown(), AiDemoConversations.Long(12));

    private void HandlePinnedChange(bool pinned)
    {
        isPinned = pinned;
    }

    private async Task ScrollToLatest()
    {
        if (followList is not null) await followList.ScrollToLatestAsync();
    }


    private readonly BitAiConversation longConversation = AiDemoConversations.Long(400);


    private string? lastEvent;
    private readonly BitAiConversation citedConversation = CreateCitedConversation();

    private static BitAiConversation CreateCitedConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.User, "Where does an Interactive Server component keep its state?");

        const string first = "On the server, for as long as the circuit lives.";
        const string second = " When the circuit drops, the state goes with it.";

        var reply = new BitAiMessage(BitAiRole.Assistant);
        reply.AddPart(new BitAiTextPart(first + second));
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

        conversation.AddMessage(reply);

        return conversation;
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
        lastEvent = $"Feedback {feedback.Feedback} on message {feedback.Message.MessageId}";
    }


    private bool isDisabled = true;
    private readonly BitAiChatSession disabledSession = new(AiDemoTransports.Echo(), CreateConversation());


    private readonly BitAiConversation styleConversation = CreateConversation();
    private readonly BitAiConversation classConversation = CreateConversation();
    private readonly BitAiConversation variablesConversation = CreateConversation();


    private readonly BitAiConversation rtlConversation = CreateRtlConversation();

    private static BitAiConversation CreateRtlConversation()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.User, "این فهرست پیام‌ها برای چیست؟");
        conversation.AddMessage(BitAiRole.Assistant, "متن کامل یک گفتگو را نشان می‌دهد: هر پیام به ترتیب، با چیدمان نقش خودش.");

        return conversation;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await session.DisposeAsync();
        await templatesSession.DisposeAsync();
        await followSession.DisposeAsync();
        await disabledSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
