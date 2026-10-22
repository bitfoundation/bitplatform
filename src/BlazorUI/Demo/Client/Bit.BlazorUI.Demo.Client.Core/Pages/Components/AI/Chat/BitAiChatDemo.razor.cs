using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.Chat;

public partial class BitAiChatDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowAttachments",
            Type = "bool",
            DefaultValue = "true",
            Description = "Offers attachments in the composer: files picked with its button, pasted or dropped onto it, up to MaxAttachmentSize each.",
        },
        new()
        {
            Name = "AllowDictation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Offers dictation in the composer: a microphone button that transcribes speech into the text, shown only where the browser can recognize speech.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiChatClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the chat.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Commands",
            Type = "IReadOnlyList<BitAiSlashCommand>?",
            DefaultValue = "null",
            Description = "The commands offered when the user types a forward slash in the composer. A command either inserts its Template into the text or runs its Handler.",
            LinkType = LinkType.Link,
            Href = "/components/ai/promptinput",
        },
        new()
        {
            Name = "ComposerHintTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A hint rendered under the composer, such as the keys that send.",
        },
        new()
        {
            Name = "ComposerPrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content placed before the text of the composer, such as a model picker.",
        },
        new()
        {
            Name = "ComposerSuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content placed beside the send button of the composer.",
        },
        new()
        {
            Name = "ComposerTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content that replaces the whole composer. Whatever it holds submits through the session (ActiveSession) itself.",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation?",
            DefaultValue = "null",
            Description = "The conversation to show, such as one restored from history. Used only when no Session is supplied - a supplied session owns its own conversation. Passing another one switches the chat to it: the session the chat created for the old one is replaced.",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist#conversation",
        },
        new()
        {
            Name = "EmptyBody",
            Type = "string?",
            DefaultValue = "null",
            Description = "The supporting line of the empty state. Defaults to the EmptyBody of the string table (\"Ask a question to get started.\").",
        },
        new()
        {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content that replaces the empty state (and with it EmptyTitle, EmptyBody and Suggestions) while the conversation has no messages.",
        },
        new()
        {
            Name = "EmptyTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The heading of the empty state. Defaults to the EmptyTitle of the string table (\"How can I help?\").",
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content below the composer, outside the scroll area, such as a disclaimer.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content above the transcript, outside the scroll area, such as a title bar.",
        },
        new()
        {
            Name = "HideSystemMessages",
            Type = "bool",
            DefaultValue = "true",
            Description = "Hides the system (and developer) messages from the transcript. Turned off, they are shown as a centered note.",
        },
        new()
        {
            Name = "ListFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content at the bottom of the scroll area, below the last message.",
        },
        new()
        {
            Name = "ListHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content at the top of the scroll area, above the first message.",
        },
        new()
        {
            Name = "MaxAttachmentSize",
            Type = "long",
            DefaultValue = "10485760",
            Description = "The largest file the composer will stage, in bytes (10 MB by default). A larger one is turned away, the user is told, and OnAttachmentRejected is raised.",
        },
        new()
        {
            Name = "Mentions",
            Type = "IReadOnlyList<BitAiMentionItem>?",
            DefaultValue = "null",
            Description = "The items offered when the user types an at sign in the composer: files, people or data sources the application resolves by their Id later. A chosen one is inserted as @Label.",
            LinkType = LinkType.Link,
            Href = "/components/ai/promptinput",
        },
        new()
        {
            Name = "MessageTemplate",
            Type = "RenderFragment<BitAiMessage>?",
            DefaultValue = "null",
            Description = "The template that replaces how a message is rendered. It is keyed by the message and adds no element of its own; render a BitAiChatMessage inside it to keep the built-in streaming, parts and actions.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chatmessage#message",
        },
        new()
        {
            Name = "OnAttachmentRejected",
            Type = "EventCallback<BitAiAttachmentRejection>",
            DefaultValue = "",
            Description = "The callback for each file the composer turns away (too large, of a type it does not take, one too many). The user is told either way; this is the same information for the application.",
            LinkType = LinkType.Link,
            Href = "/components/ai/promptinput",
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
            Description = "The callback for when the user records feedback on a reply (thumbs up or down, or withdrawn), so a host can persist it. The message's Feedback is already updated when it is raised.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chatmessage#message-feedback",
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
            Name = "OnSubmit",
            Type = "EventCallback<BitAiPromptSubmission>",
            DefaultValue = "",
            Description = "The callback for when the user submits (the composer or a suggestion). Handle it to submit yourself - after uploading the attachments, or adding context to the text - through the session; leave it unset and the chat submits to the session.",
            LinkType = LinkType.Link,
            Href = "/components/ai/promptinput",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder of the composer, which also names it when no AriaLabel is given. Defaults to the PromptPlaceholder of the string table (\"Send a message...\").",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session to drive. When unset, the chat creates one - through the registered BitAiChatSessionFactory, or over the registered IBitAiTransport - and disposes it with itself; a supplied session belongs to the page, which disposes it.",
            LinkType = LinkType.Link,
            Href = "#chat-session",
        },
        new()
        {
            Name = "SessionOptions",
            Type = "BitAiChatSessionOptions?",
            DefaultValue = "null",
            Description = "The settings of a session this component creates. Ignored when a Session is supplied, which carries its own.",
            LinkType = LinkType.Link,
            Href = "#session-options",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiChatClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the chat.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Suggestions",
            Type = "IReadOnlyList<string>?",
            DefaultValue = "null",
            Description = "The example prompts offered in the empty state. A tap submits one as it is.",
        },
        new()
        {
            Name = "UiOptions",
            Type = "BitAiUiOptions?",
            DefaultValue = "null",
            Description = "The presentation settings of everything inside this chat (the string table, the render interval, what a message shows), in place of the ones a BitAiOptionsProvider cascades or AddBitBlazorUIAIServices registers.",
            LinkType = LinkType.Link,
            Href = "/components/ai/optionsprovider",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ActiveSession",
            Type = "BitAiChatSession",
            DefaultValue = "",
            Description = "The session in use, whether supplied through Session or created by the chat. Reading it before the first render creates the chat's own session.",
            LinkType = LinkType.Link,
            Href = "#chat-session",
        },
        new()
        {
            Name = "ActiveConversation",
            Type = "BitAiConversation",
            DefaultValue = "",
            Description = "The conversation in use, the one of ActiveSession.",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist#conversation",
        },
        new()
        {
            Name = "FocusComposerAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Puts the caret in the composer. Does nothing when a ComposerTemplate replaces it.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiChat-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Background of the chat, which the footer fades the last lines of the transcript into." },
        new() { Name = "--bit-AiChat-max-width", DefaultValue = "spacing(92)", Description = "Width the composer stops growing at. Set --bit-AiMessageList-max-width to the same value to keep it lined up with the column of messages." },
        new() { Name = "--bit-AiChat-footer-padding", DefaultValue = "0 spacing(2) spacing(2), 0 spacing(1.5) spacing(1.5) on a phone", Description = "Inset of the footer around the composer." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiChatClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiChat."
                },
                new()
                {
                    Name = "MessageList",
                    Type = "BitAiMessageListClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiMessageList (the transcript) of the BitAiChat.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messagelist#class-styles"
                },
                new()
                {
                    Name = "EmptyState",
                    Type = "BitAiEmptyStateClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiEmptyState of the BitAiChat.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/emptystate#class-styles"
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitAiChat, which holds the composer."
                },
                new()
                {
                    Name = "FooterColumn",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the column inside the footer of the BitAiChat, which lines the composer up with the messages above it."
                },
                new()
                {
                    Name = "ScrollToLatest",
                    Type = "BitAiScrollToLatestClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiScrollToLatest of the BitAiChat.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/scrolltolatest#class-styles"
                }
            ]
        },
        new()
        {
            Id = "chat-session",
            Title = "BitAiChatSession",
            Description = "Drives one conversation: submission, streaming, tool execution, approval, cancellation, retry and regeneration. Create one with the injected BitAiChatSessionFactory (Create(conversation, options)), which brings the registered transport, tools and approval policy, or with new BitAiChatSession(transport, conversation, options, ...). Submitting while a turn is in flight is a no-op. Dispose it (DisposeAsync) when the page goes: that cancels the turn in flight and detaches every handler.",
            Parameters =
            [
                new()
                {
                    Name = "Conversation",
                    Type = "BitAiConversation",
                    DefaultValue = "",
                    Description = "The conversation this session drives, given to its constructor or created with it.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messagelist#conversation"
                },
                new()
                {
                    Name = "Options",
                    Type = "BitAiChatSessionOptions",
                    DefaultValue = "new()",
                    Description = "The settings in force.",
                    LinkType = LinkType.Link,
                    Href = "#session-options"
                },
                new()
                {
                    Name = "IsGenerating",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True while a turn is in flight."
                },
                new()
                {
                    Name = "LastError",
                    Type = "BitAiError?",
                    DefaultValue = "null",
                    Description = "The failure of the last turn, cleared when a new turn starts.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/errorview"
                },
                new()
                {
                    Name = "SubmitAsync(text, attachments?, cancellationToken?)",
                    Type = "Task<BitAiMessage?>",
                    DefaultValue = "",
                    Description = "Appends a user message (the text may be empty when attachments carry the request) and generates a reply. Returns the assistant message, or null when a turn was already running."
                },
                new()
                {
                    Name = "RegenerateAsync(messageId?, cancellationToken?)",
                    Type = "Task<BitAiMessage?>",
                    DefaultValue = "",
                    Description = "Regenerates an assistant message - the most recent one when no id is given - discarding it and everything after it."
                },
                new()
                {
                    Name = "RetryAsync(cancellationToken?)",
                    Type = "Task<BitAiMessage?>",
                    DefaultValue = "",
                    Description = "Retries the last turn after a failure, keeping the user message in place."
                },
                new()
                {
                    Name = "EditAsync(messageId, text, cancellationToken?)",
                    Type = "Task<BitAiMessage?>",
                    DefaultValue = "",
                    Description = "Replaces the text of a user message and regenerates everything after it."
                },
                new()
                {
                    Name = "Stop()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Stops the current turn. The partial answer is kept and marked cancelled. Safe to call from any thread, and a no-op with no turn in flight."
                },
                new()
                {
                    Name = "Approve(toolCallId, decision)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Records a human decision on a tool call that is waiting for one.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/toolapproval"
                },
                new()
                {
                    Name = "RespondToAgentAsync(runId, answer, cancellationToken?)",
                    Type = "Task<BitAiMessage?>",
                    DefaultValue = "",
                    Description = "Answers a question an agent run is blocked on (appended as a user message), then continues the turn.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentrunview"
                },
                new()
                {
                    Name = "StateChanged",
                    Type = "event Action?",
                    DefaultValue = "",
                    Description = "Raised whenever anything the UI cares about changes. Coarse by design and raised from whatever thread the stream is on: marshal it with InvokeAsync, and coalesce the renders it asks for."
                },
                new()
                {
                    Name = "TurnCompleted",
                    Type = "event Action<BitAiMessage>?",
                    DefaultValue = "",
                    Description = "Raised when a turn ends, successfully or not, with its assistant message."
                },
                new()
                {
                    Name = "ApprovalRequested",
                    Type = "event Action<BitAiToolCallPart>?",
                    DefaultValue = "",
                    Description = "Raised when a tool call needs a human decision."
                }
            ]
        },
        new()
        {
            Id = "session-options",
            Title = "BitAiChatSessionOptions",
            Description = "The settings of a BitAiChatSession, an immutable record. Every one has a defensible default: tools execute automatically, destructive ones ask first, and a runaway tool loop is bounded. AddBitBlazorUIAIServices sets the defaults of the sessions its factory creates (ai.ConfigureSessions).",
            Parameters =
            [
                new()
                {
                    Name = "AutoTitle",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Derives the title of the conversation from the first user message when it has none."
                },
                new()
                {
                    Name = "CatchTransportExceptions",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Leaves a retryable error on the assistant message when a turn fails, rather than throwing."
                },
                new()
                {
                    Name = "ExecuteTools",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Executes the tool calls of the model in the session. Turn it off when the transport runs the tools on the server and only reports them as events."
                },
                new()
                {
                    Name = "MaxToolRoundtrips",
                    Type = "int",
                    DefaultValue = "10",
                    Description = "How many times the session feeds tool results back and asks the model again in one turn - the bound that keeps a model calling tools in a loop from spending without end."
                },
                new()
                {
                    Name = "Pacing",
                    Type = "BitAiTextPacingOptions?",
                    DefaultValue = "BitAiTextPacingOptions.Default",
                    Description = "Evens out bursty text output so it renders at a steady rate. Null renders the deltas exactly as the transport produces them; contradicting settings throw when the options are built."
                },
                new()
                {
                    Name = "RequestOptions",
                    Type = "BitAiRequestOptions?",
                    DefaultValue = "null",
                    Description = "The generation settings (the model, the temperature, the token limit, ...) passed to the transport on every request."
                },
                new()
                {
                    Name = "Timeout",
                    Type = "TimeSpan?",
                    DefaultValue = "null",
                    Description = "How long to wait for the whole turn, tool round trips included. Null sets no limit beyond what the transport enforces."
                }
            ]
        }
    ];



    private readonly string[] suggestions =
    [
        "What can these components do?",
        "Show me streaming",
        "Explain render modes",
    ];


    private BitAiChat? sessionChat;
    private readonly BitAiChatSession markdownSession = new(AiDemoTransports.Markdown());

    private async Task AskFromCode()
    {
        await markdownSession.SubmitAsync("Where does Blazor run my code?");
    }

    private async Task FocusComposer()
    {
        if (sessionChat is not null) await sessionChat.FocusComposerAsync();
    }


    private bool hideSystemMessages = true;
    private readonly BitAiConversation history = CreateHistory();
    private readonly BitAiChatSessionOptions historyOptions = new() { AutoTitle = false, Timeout = TimeSpan.FromSeconds(30) };

    private static BitAiConversation CreateHistory()
    {
        var conversation = new BitAiConversation();

        conversation.AddMessage(BitAiRole.System, "You are the assistant of the bit BlazorUI docs.");
        conversation.AddMessage(BitAiRole.User, "Which package are the AI components in?");
        conversation.AddMessage(BitAiRole.Assistant, "In **Bit.BlazorUI.AI**, which builds on the core Bit.BlazorUI package.");

        return conversation;
    }


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

        await templatesChat.ActiveSession.SubmitAsync("How do refunds work?");
    }


    private BitAiChat? composerChat;
    private string? modelId = AiDemoCatalog.DefaultModelId;

    private void ClearComposerChat()
    {
        if (composerChat is null) return;

        composerChat.ActiveSession.Stop();
        composerChat.ActiveConversation.Clear();
    }


    private readonly BitAiChatSession guidedSession = new(AiDemoTransports.Echo());
    private readonly string[] guidedReplies = ["Track my order", "Change my address", "Talk to a person"];


    private bool allowAttachments = true;
    private bool allowDictation;
    private bool offerCommands = true;
    private string? rejectedFile;
    private readonly BitAiChatSession commandsSession = new(AiDemoInteractiveTransports.Commands());
    private IReadOnlyList<BitAiSlashCommand> commands = [];
    private readonly string[] commandSuggestions = ["Compare @roadmap.md with @pricing.xlsx", "Ask @priya about the launch date"];

    private readonly IReadOnlyList<BitAiMentionItem> mentions =
    [
        new("file-roadmap", "roadmap.md", "Product roadmap, updated Monday", BitIconName.Page),
        new("file-pricing", "pricing.xlsx", "Pricing model, three tiers", BitIconName.Page),
        new("user-priya", "priya", "Priya Raman, product lead", BitIconName.Contact),
        new("source-analytics", "analytics", "Usage analytics, last 30 days", BitIconName.Database),
    ];

    private void HandleAttachmentRejected(BitAiAttachmentRejection rejection)
    {
        rejectedFile = $"{rejection.FileName} ({rejection.Reason})";
    }


    private string? lastSubmission;
    private readonly BitAiChatSession submitSession = new(AiDemoTransports.Echo());

    private async Task HandleSubmit(BitAiPromptSubmission submission)
    {
        lastSubmission = $"\"{submission.Text}\" with {submission.Attachments.Count} attachment(s)";

        // Upload the attachments to your own storage here, or add what the model should know - this adds the page.
        await submitSession.SubmitAsync($"[asked on the AiChat page] {submission.Text}", submission.Attachments);
    }


    private readonly BitAiChatSession templateSession = new(AiDemoTransports.Echo());


    private string? lastEvent;
    private readonly BitAiChatSession citedSession = new(AiDemoContentTransports.Cited());
    private readonly string[] citedSuggestions = ["What happens to my state when the circuit drops?"];

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


    private readonly BitAiChatSession reasoningSession = new(AiDemoTransports.Reasoning());
    private readonly BitAiUiOptions uiOptions = new() { CollapseReasoning = false, ShowTimestamps = true, ShowUsage = true };
    private readonly string[] reasoningSuggestions = ["How long does a round trip to the model take?"];


    private bool isDisabled = true;
    private readonly BitAiConversation disabledHistory = CreateHistory();


    private readonly BitAiChatParams[] chatParams =
    [
        new()
        {
            EmptyTitle = "Ask the docs",
            EmptyBody = "Both chats take this heading and line from the BitParams around them.",
            Placeholder = "Ask about a component...",
            AllowAttachments = false,
        }
    ];


    private readonly string[] rtlSuggestions = ["این کامپوننت‌ها چه کارهایی می‌کنند؟", "استریم را نشانم بده"];



    protected override Task OnInitAsync()
    {
        commands =
        [
            new("summarize", "Summarize the conversation so far", BitIconName.BulletedList)
            {
                Template = "Summarize the conversation so far in three bullet points.",
            },
            new("clear", "Start over with an empty conversation", BitIconName.Delete)
            {
                Handler = () =>
                {
                    commandsSession.Stop();
                    commandsSession.Conversation.Clear();
                    return Task.CompletedTask;
                },
            },
        ];

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        // The sessions this page created are its own to end; the chats dispose only the ones they created themselves.
        await markdownSession.DisposeAsync();
        await guidedSession.DisposeAsync();
        await commandsSession.DisposeAsync();
        await submitSession.DisposeAsync();
        await templateSession.DisposeAsync();
        await citedSession.DisposeAsync();
        await reasoningSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
