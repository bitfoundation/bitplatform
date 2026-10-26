using System.Text.Json;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MessagePartView;

public partial class BitAiMessagePartViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiMessagePartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the message part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation?",
            DefaultValue = "null",
            Description = "The conversation, when the part is rendered inside one. An artifact part is resolved against it (the artifact lives on the conversation, the part only points at it), and a registered part renderer is handed it (BitAiMessagePartContext.Conversation).",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist#conversation",
        },
        new()
        {
            Name = "Message",
            Type = "BitAiMessage",
            DefaultValue = "",
            Description = "The message the part belongs to (required). A text part reads its citations from it, a tool call and structured data hand it on, and a registered part renderer is handed it.",
        },
        new()
        {
            Name = "OnCitationClick",
            Type = "EventCallback<BitAiCitation>",
            DefaultValue = "",
            Description = "Raised when the user activates a citation inside this view: a citation chip of a text part, or the card of a citation part. While it is not set, the card of a citation part is a link to its source instead.",
        },
        new()
        {
            Name = "Part",
            Type = "BitAiMessagePart",
            DefaultValue = "",
            Description = "The part to render (required), drawn by the view of its kind - or by the renderer registered for its Kind, which is looked up again every time the view's parameters are set. The view subscribes to the part and re-renders whenever it changes, at most once per BitAiUiOptions.RenderInterval; handing it another part moves the subscription to that one.",
            LinkType = LinkType.Link,
            Href = "#message-part",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session driving the conversation, when there is one. A tool call hands its approval decisions to it, and a registered part renderer is handed it (BitAiMessagePartContext.Session), so it can act as well as display.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiMessagePartViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the message part view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiMessagePartViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiMessagePartView.",
                },
                new()
                {
                    Name = "Fallback",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text the BitAiMessagePartView shows for a part of a kind it does not know.",
                },
                new()
                {
                    Name = "Citation",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of a citation part of the BitAiMessagePartView.",
                },
                new()
                {
                    Name = "CitationCard",
                    Type = "BitAiSourceCardClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiSourceCard of a citation part of the BitAiMessagePartView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/sourcecard#class-styles",
                },
                new()
                {
                    Name = "CitationQuote",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the quote of a citation part of the BitAiMessagePartView.",
                },
                new()
                {
                    Name = "CitationLocator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the locator (a page, a section) of a citation part of the BitAiMessagePartView.",
                },
            ],
        },
        new()
        {
            Id = "message-part",
            Title = "BitAiMessagePart",
            Description = "The base class of every part of a message, with an identity and a change notification of its own. Only the part that grew raises Changed while a reply streams, which is what lets one view re-render per token instead of the whole message.",
            Parameters =
            [
                new()
                {
                    Name = "PartId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the part, generated unless the transport addresses parts explicitly. A good @key for its view.",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The discriminator the view is chosen by, and the one a part renderer is registered against. The built-in kinds are the BitAiMessagePartKinds constants; a custom part takes any other.",
                    LinkType = LinkType.Link,
                    Href = "#part-kinds",
                },
                new()
                {
                    Name = "Version",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Increments on every change, for a cheap ShouldRender of a view of the app's own: compare it with the version seen at the last render.",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage?",
                    DefaultValue = "null",
                    Description = "The message the part belongs to, once it has been added to one.",
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitAiMessagePart>",
                    DefaultValue = "",
                    Description = "Raised after the part changes, synchronously on the thread that changed it - a component marshals onto the renderer with InvokeAsync, which the built-in views do for you.",
                },
            ],
        },
        new()
        {
            Id = "part-kinds",
            Title = "BitAiMessagePartKinds",
            Description = "The Kind constants the library ships with, and what this view renders each as. Use them, rather than string literals, when registering a part renderer.",
            Parameters =
            [
                new() { Name = "Text", Type = "\"text\"", DefaultValue = "", Description = "A BitAiTextPart, the ordinary body of a reply: rendered as streaming Markdown by a BitAiTextPartView, with its citation chips." },
                new() { Name = "Markdown", Type = "\"markdown\"", DefaultValue = "", Description = "A BitAiMarkdownPart, text explicitly marked as Markdown: rendered like Text." },
                new() { Name = "Reasoning", Type = "\"reasoning\"", DefaultValue = "", Description = "A BitAiReasoningPart, the model's reasoning: a BitAiReasoningPartView, collapsed unless CollapseReasoning is turned off." },
                new() { Name = "Code", Type = "\"code\"", DefaultValue = "", Description = "A BitAiCodePart, a standalone code block with a language and an optional file name: a BitAiCodeBlockView." },
                new() { Name = "Image", Type = "\"image\"", DefaultValue = "", Description = "A BitAiImagePart: a BitAiImagePartView." },
                new() { Name = "File", Type = "\"file\"", DefaultValue = "", Description = "A BitAiFilePart: a BitAiAttachmentChip of its attachment." },
                new() { Name = "Citation", Type = "\"citation\"", DefaultValue = "", Description = "A BitAiCitationPart, a single citation placed where it sits between the parts: the BitAiSourceCard of its source, numbered as the message numbers it, with the Quote and the Locator of the citation under it. The card raises OnCitationClick when that is set, and links to the source otherwise." },
                new() { Name = "Sources", Type = "\"sources\"", DefaultValue = "", Description = "A BitAiSourcesPart, where the source list of the message goes: a BitAiSourceList of the citations it names, or of all the message's citations." },
                new() { Name = "ToolCall", Type = "\"tool-call\"", DefaultValue = "", Description = "A BitAiToolCallPart: a BitAiToolCallView, or the renderer registered for its tool." },
                new() { Name = "ToolResult", Type = "\"tool-result\"", DefaultValue = "", Description = "A BitAiToolResultPart, a result carried on its own rather than on its call (the stream processor never adds one; an app does, for a conversation it imports): a BitAiToolResultView." },
                new() { Name = "Artifact", Type = "\"artifact\"", DefaultValue = "", Description = "A BitAiArtifactPart, pointing at an artifact of the Conversation: a BitAiArtifactPartView." },
                new() { Name = "StructuredData", Type = "\"structured-data\"", DefaultValue = "", Description = "A BitAiStructuredDataPart, a JSON value that may still be streaming: a BitAiStructuredDataView, or the renderer registered for its schema." },
                new() { Name = "GenerativeUi", Type = "\"generative-ui\"", DefaultValue = "", Description = "A BitAiGenerativeUiPart, a component the model chose with the properties it supplied: a BitAiGenerativeUiView." },
                new() { Name = "Form", Type = "\"form\"", DefaultValue = "", Description = "A form the user fills in and sends back. No built-in part carries this kind: a form arrives as structured data (rendered with BitAiForm by a structured renderer), or as a custom part of this kind with a part renderer registered for it." },
                new() { Name = "Error", Type = "\"error\"", DefaultValue = "", Description = "A BitAiErrorPart, a failure inside an answer that is otherwise worth keeping: a BitAiErrorView." },
            ],
        },
        new()
        {
            Id = "custom-part",
            Title = "BitAiCustomPart<T> / BitAiCustomJsonPart",
            Description = "The parts for content the library has no concept of. Give one a kind of its own and register a renderer for that kind: it is then rendered, copied and virtualized like a built-in part. A kind with no renderer is shown as its Text.",
            Parameters =
            [
                new()
                {
                    Name = "BitAiCustomPart<T>(kind, data, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "A part carrying a typed payload, created by the app.",
                },
                new()
                {
                    Name = "BitAiCustomJsonPart(kind, data, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "A part carrying a JsonElement payload, for one that arrives as JSON - from a model or a transport.",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The discriminator a renderer is registered against. Namespace it (acme:invoice) so it never collides with a built-in kind.",
                },
                new()
                {
                    Name = "Data",
                    Type = "T / JsonElement",
                    DefaultValue = "",
                    Description = "The payload. Setting Data of a BitAiCustomPart<T> notifies its view; after changing the payload in place, call its Refresh(). The data of a BitAiCustomJsonPart is fixed.",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The plain-text projection of the part, used when it is copied or exported - and shown by this view when no renderer is registered for the kind.",
                },
            ],
        },
        new()
        {
            Id = "renderer-registry",
            Title = "BitAiRendererRegistry",
            Description = "Where an app registers its own renderers, which the components resolve before their built-in views. A singleton: register at startup (AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(renderers => ...))) and treat the entries as fixed afterwards - every entry is app-wide. A region with renderers of its own cascades a registry of its own (new BitAiRendererRegistry(), in a CascadingValue), which every AI component inside it uses in place of the registered one - in place of it, not on top of it. These are its part members; RegisterTool is on the AiToolCallView page, RegisterArtifact on the AiArtifactView page, RegisterStructured on the AiStructuredDataView page, and RegisterCodeLanguage, MathRenderer and CitationRenderer on the AiMarkdownView page.",
            Parameters =
            [
                new()
                {
                    Name = "RegisterPart(kind, renderer)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Registers a BitAiMessagePartRenderer for a part kind (case does not matter). Every BitAiMessagePartView of a part of that kind renders it in place of its built-in view, inside a BitAiRendererBoundary - a built-in kind included; a later registration replaces an earlier one.",
                    LinkType = LinkType.Link,
                    Href = "#message-part-renderer",
                },
                new()
                {
                    Name = "RegisterPart<TComponent>(kind)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Registers a component as the renderer of a part kind. The component is handed the part as a parameter named Part; declare it with the part type it expects (BitAiCustomJsonPart, BitAiCustomPart<Invoice>).",
                },
                new()
                {
                    Name = "FindPart(kind)",
                    Type = "BitAiMessagePartRenderer?",
                    DefaultValue = "",
                    Description = "The renderer registered for a part kind, or null when the built-in view draws it.",
                },
            ],
        },
        new()
        {
            Id = "message-part-context",
            Title = "BitAiMessagePartContext",
            Description = "What a custom part renderer is handed (a sealed record). The session is included so a renderer can act as well as display - a custom approval card can call back into the session without the app threading state through cascading values.",
            Parameters =
            [
                new()
                {
                    Name = "Part",
                    Type = "BitAiMessagePart",
                    DefaultValue = "",
                    Description = "The part to render.",
                    LinkType = LinkType.Link,
                    Href = "#message-part",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage",
                    DefaultValue = "",
                    Description = "The message the part belongs to.",
                },
                new()
                {
                    Name = "Conversation",
                    Type = "BitAiConversation?",
                    DefaultValue = "null",
                    Description = "The conversation, when the part is rendered inside one.",
                },
                new()
                {
                    Name = "Session",
                    Type = "BitAiChatSession?",
                    DefaultValue = "null",
                    Description = "The session driving the conversation, when there is one.",
                },
            ],
        },
        new()
        {
            Id = "message-part-renderer",
            Title = "BitAiMessagePartRenderer",
            Description = "The delegate a custom part renderer is: public delegate RenderFragment BitAiMessagePartRenderer(BitAiMessagePartContext context). It is called every time the view's parameters are set, so it builds markup from the context rather than caching it.",
            Parameters =
            [
                new()
                {
                    Name = "context",
                    Type = "BitAiMessagePartContext",
                    DefaultValue = "",
                    Description = "The part and its surroundings.",
                    LinkType = LinkType.Link,
                    Href = "#message-part-context",
                },
                new()
                {
                    Name = "(return value)",
                    Type = "RenderFragment",
                    DefaultValue = "",
                    Description = "The markup drawn in place of the built-in view. For a failure to be contained by the boundary, the code that can throw has to run inside a component the fragment renders, as RegisterPart<TComponent> does.",
                },
            ],
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiMessagePartView-citation-gap", DefaultValue = "spacing(0.75)", Description = "Room between the card, the quote and the locator of a citation part." },
        new() { Name = "--bit-AiMessagePartView-quote-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the quote of a citation part." },
        new() { Name = "--bit-AiMessagePartView-quote-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Bar down the start edge of the quote of a citation part." },
        new() { Name = "--bit-AiMessagePartView-locator-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the locator (a page, a section) of a citation part." },
    ];



    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    private static BitAiToolCallPart WeatherCall()
    {
        var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), "get_weather")
        {
            Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
        };

        call.CompleteArguments(Json("""{"city":"Oslo"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(BitAiToolOutcome.Success(Json("""{"city":"Oslo","degrees":-3,"condition":"Snow"}""")) with
        {
            Duration = TimeSpan.FromMilliseconds(700),
        });

        return call;
    }

    private readonly BitAiMessage basicMessage = CreateBasicMessage();

    private static BitAiMessage CreateBasicMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart("Let me check the weather in Oslo."));
        message.AddPart(WeatherCall());
        message.AddPart(new BitAiTextPart("It is **-3 °C** and snowing in Oslo - bring a warm coat."));

        return message;
    }



    private const string packingList = """
        Pack for **-3 °C** and snow:

        - a warm coat and a hat
        - gloves and a scarf
        - boots with a good grip

        The days are short in winter, so plan to be indoors by four.
        """;
    private BitAiMessage streamingMessage = CreateStreamingMessage();
    private bool isStreaming;

    private static BitAiMessage CreateStreamingMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(WeatherCall());

        return message;
    }

    private async Task StreamAsync()
    {
        isStreaming = true;
        streamingMessage = CreateStreamingMessage();

        // An empty text part expects more text, so it streams until Complete is called.
        var answer = streamingMessage.AddPart(new BitAiTextPart());
        StateHasChanged();

        foreach (var chunk in packingList.Chunk(4))
        {
            await Task.Delay(25);
            answer.Append(new string(chunk));
        }

        answer.Complete();
        isStreaming = false;
    }



    private readonly BitAiMessage kindsMessage = CreateKindsMessage();

    private static BitAiMessage CreateKindsMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiReasoningPart("The user wants the setup in one line. AddBitBlazorUIAIServices with a transport is all it takes."));
        message.AddPart(new BitAiCodePart("builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());", "csharp") { FileName = "Program.cs" });
        message.AddPart(new BitAiToolResultPart("call_logs", BitAiToolOutcome.FromText("No errors in the last hour.")) { ToolName = "read_logs" });
        message.AddPart(new BitAiErrorPart(new BitAiError("The second search timed out, so this answer is based on the first one only.")
        {
            Kind = BitAiErrorKind.Timeout,
        }));
        message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["Tuesday", "Thursday"]) { Text = "Poll: which day suits you, Tuesday or Thursday?" });

        return message;
    }



    private readonly BitAiMessage citedMessage = CreateCitedMessage();
    private string? clickedCitation;

    private static BitAiMessage CreateCitedMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddCitation(new BitAiCitation("cit_modes", new BitAiCitationSource("render-modes", "Render modes")
        {
            Url = "https://example.com/docs/render-modes",
            SiteName = "Product documentation",
        }));
        var prerendering = message.AddCitation(new BitAiCitation("cit_prerender", new BitAiCitationSource("prerendering", "Prerendering")
        {
            Url = "https://example.com/docs/prerendering",
            SiteName = "Product documentation",
        })
        {
            Quote = "Prerendering renders the page on the server first, so it is painted before its components become interactive.",
            Locator = "Section 3: Prerendering",
        });
        message.AddPart(new BitAiTextPart("Interactive Server keeps the components on the server [1], and prerendering paints them once before they become interactive [2]."));
        message.AddPart(new BitAiCitationPart(prerendering));

        return message;
    }



    [AutoInject] private IBitAiToolRegistry toolRegistry = default!;

    private BitAiRenderScheduler sessionRenders = default!;
    private BitAiChatSession planSession = default!;

    // A conversation whose first answer wrote an artifact, the way a session leaves it: the artifact belongs to the
    // conversation, and the part of the answer only names it.
    private static BitAiConversation CreatePlanConversation()
    {
        var conversation = new BitAiConversation();

        var plan = conversation.GetOrAddArtifact("art-cleanup", BitAiArtifactKind.Document, "Clean-up plan");
        plan.SetContent("""
            ## Clean-up plan

            1. Archive the records nobody opened this year.
            2. Delete the archived records.
            """);
        plan.Complete("First draft");

        conversation.AddMessage(BitAiRole.User, "Plan the clean-up of the old records.");

        var answer = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
        answer.AddPart(new BitAiTextPart("Here is the plan:"));
        answer.AddPart(new BitAiArtifactPart("art-cleanup"));

        return conversation;
    }

    private Task AskToDeleteAsync() => planSession.SubmitAsync("Delete the archived records");



    [AutoInject] private BitAiRendererRegistry renderers = default!;

    protected override void OnInitialized()
    {
        // An app registers its renderers once, at startup (the Program.cs tab). The registry is shared by the whole app, so
        // this page registers its own kind as it loads - the same call, under a kind nothing else uses.
        renderers.RegisterPart<InvoiceCard>("demo:invoice");

        // The session of the Conversation & Session example raises StateChanged on every event it applies; the scheduler
        // turns those into at most one render of the page per frame interval. A scripted model (Services/AI) asks for
        // delete_records, then answers once the tool has returned; the tool is the one registered with
        // AddBitBlazorUIAIServices, declared Destructive, so the default approval policy asks before it runs.
        sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));
        planSession = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "delete_records",
                """{"filter":"status = 'archived'"}""",
                "Deleted 128 archived records."),
            conversation: CreatePlanConversation(),
            tools: toolRegistry,
            approvalPolicy: new BitAiDefaultToolApprovalPolicy());
        planSession.StateChanged += sessionRenders.Request;

        base.OnInitialized();
    }

    public async ValueTask DisposeAsync()
    {
        if (planSession is not null) await planSession.DisposeAsync();
        if (sessionRenders is not null) await sessionRenders.DisposeAsync();
    }

    private const string octoberInvoice = """
        {"number":"INV-1042","currency":"EUR","lines":[{"item":"Design review","amount":1200},{"item":"Hosting, 3 months","amount":90}]}
        """;

    private readonly BitAiMessage invoiceMessage = CreateInvoiceMessage();

    private static BitAiMessage CreateInvoiceMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart("Here is the invoice for October:"));
        message.AddPart(new BitAiCustomJsonPart("demo:invoice", Json(octoberInvoice)) { Text = "Invoice INV-1042: 1,290.00 EUR" });

        return message;
    }



    // The model left the lines out of the second invoice, which the card was not written for.
    private const string novemberInvoice = """{"number":"INV-1043","currency":"EUR","total":1290}""";

    private readonly BitAiMessage brokenMessage = CreateBrokenMessage();

    private static BitAiMessage CreateBrokenMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart("Here are the invoices for October and November:"));
        message.AddPart(new BitAiCustomJsonPart("demo:invoice", Json(octoberInvoice)) { Text = "Invoice INV-1042: 1,290.00 EUR" });
        message.AddPart(new BitAiCustomJsonPart("demo:invoice", Json(novemberInvoice)) { Text = "Invoice INV-1043: 1,290.00 EUR" });
        message.AddPart(new BitAiTextPart("Both are due at the end of the month."));

        return message;
    }



    private readonly BitAiMessage pollMessage = CreatePollMessage();

    private static BitAiMessage CreatePollMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart("Let us find a day for the review."));
        message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["Tuesday", "Thursday"]) { Text = "Poll: which day suits you, Tuesday or Thursday?" });

        return message;
    }

    // A registry of this region's own: the AI components inside the CascadingValue use it in place of the registered one,
    // so it holds every renderer the region needs - here only the poll.
    private readonly BitAiRendererRegistry regionRenderers = CreateRegionRenderers();

    private static BitAiRendererRegistry CreateRegionRenderers()
    {
        var registry = new BitAiRendererRegistry();

        registry.RegisterPart("demo:poll", RenderPoll);

        return registry;
    }

    // A BitAiMessagePartRenderer: handed the part and its surroundings (context.Message, context.Conversation and
    // context.Session as well), it returns the markup drawn in place of the built-in view. The choices are model output,
    // so they are only ever written as text.
    private static RenderFragment RenderPoll(BitAiMessagePartContext context) => builder =>
    {
        if (context.Part is not BitAiCustomPart<string[]> poll) return;

        builder.OpenElement(0, "fieldset");
        builder.AddAttribute(1, "style", "margin: 0; padding: 0.75rem 1rem; display: flex; gap: 0.5rem; flex-direction: column; border: 1px solid var(--bit-clr-brd-sec); border-radius: 0.5rem;");
        builder.OpenElement(2, "legend");
        builder.AddAttribute(3, "style", "padding-inline: 0.25rem; font-weight: 600;");
        builder.AddContent(4, "Which day suits you?");
        builder.CloseElement();

        foreach (var choice in poll.Data)
        {
            builder.OpenElement(5, "label");
            builder.AddAttribute(6, "style", "display: flex; gap: 0.5rem; align-items: center;");
            builder.OpenElement(7, "input");
            builder.AddAttribute(8, "type", "radio");
            builder.AddAttribute(9, "name", poll.PartId);
            builder.AddAttribute(10, "value", choice);
            builder.CloseElement();
            builder.AddContent(11, choice);
            builder.CloseElement();
        }

        builder.CloseElement();
    };



    private readonly BitAiMessagePartViewParams[] partViewParams =
    [
        new()
        {
            Style = "padding-inline-start: 0.75rem; border-inline-start: 3px solid var(--bit-clr-pri);",
            Styles = new() { CitationQuote = "color: var(--bit-clr-pri);", CitationLocator = "font-weight: bold;" },
        },
    ];



    private readonly BitAiMessage styleMessage = CreateStyleMessage();

    private static BitAiMessage CreateStyleMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        var forecast = message.AddCitation(new BitAiCitation("cit_forecast", new BitAiCitationSource("forecast", "Oslo forecast")
        {
            Url = "https://example.com/weather/oslo",
            SiteName = "Weather service",
        })
        {
            Quote = "Snow showers through the afternoon, -3 °C.",
            Locator = "Today, 14:00",
        });
        message.AddPart(new BitAiTextPart("It is **-3 °C** and snowing in Oslo."));
        message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["Tuesday", "Thursday"]) { Text = "Poll: which day suits you, Tuesday or Thursday?" });
        message.AddPart(new BitAiCitationPart(forecast));

        return message;
    }



    private readonly BitAiMessage rtlMessage = CreateRtlMessage();

    private static BitAiMessage CreateRtlMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);

        message.AddPart(new BitAiTextPart("هوای تهران امروز **۱۸ درجه** و آفتابی است."));
        message.AddPart(new BitAiCustomPart<string[]>("demo:poll", ["سه‌شنبه", "پنج‌شنبه"]) { Text = "نظرسنجی: کدام روز برایتان مناسب است، سه‌شنبه یا پنج‌شنبه؟" });

        return message;
    }
}
