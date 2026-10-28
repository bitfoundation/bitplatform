using System.Text.Json;
using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ToolCallView;

public partial class BitAiToolCallViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Call",
            Type = "BitAiToolCallPart",
            DefaultValue = "",
            Description = "The tool call to render (required): its tool, arguments, status, progress, result and approval. The card subscribes to the call and re-renders itself whenever it changes - at most once per BitAiUiOptions.RenderInterval - so nothing has to re-render it while the call streams or runs. Its Definition names the tool (Title, else the tool name) and picks the icon of the header (Icon) - until a session sets it, the definition registered for the tool does - and the Risk of the Definition decides how an approval prompt looks.",
            LinkType = LinkType.Link,
            Href = "#tool-call-part",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiToolCallViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tool call view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Message",
            Type = "BitAiMessage?",
            DefaultValue = "null",
            Description = "The message the call belongs to. Only a custom tool renderer reads it (BitAiToolCallContext.Message); left unset, the renderer is handed an empty assistant message.",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session driving the conversation. The approval prompt of a call awaiting a decision hands the decision to it (BitAiChatSession.Approve), which is what lets the paused turn carry on; without one the decision is only recorded on the call. A custom tool renderer is handed it as well (BitAiToolCallContext.Session).",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiToolCallViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tool call view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiToolCallViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header (the button that expands and collapses the card) of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "ExpanderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the expander chevron of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tool icon of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Status",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the status (the busy dots, or the duration of a finished call) of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Spinner",
                    Type = "BitLoadingClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the busy dots (a BitBouncingDotsLoading) of a running call of the BitAiToolCallView.",
                    LinkType = LinkType.Link,
                    Href = "/components/loading#class-styles",
                },
                new()
                {
                    Name = "Progress",
                    Type = "BitProgressClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the progress bar (a BitProgress) of the BitAiToolCallView.",
                    LinkType = LinkType.Link,
                    Href = "/components/progress#progressBar-class-styles",
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "ProgressMessage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the progress message in the body of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Section",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each section (the arguments, the result) of the body of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label (the heading) of each section of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "RawText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the raw text of the arguments (before they parse) or of a textual result of the BitAiToolCallView.",
                },
                new()
                {
                    Name = "Approval",
                    Type = "BitAiToolApprovalClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the approval prompt (a BitAiToolApproval) of the BitAiToolCallView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/toolapproval#class-styles",
                },
            ],
        },
        new()
        {
            Id = "tool-call-part",
            Title = "BitAiToolCallPart",
            Description = "A tool call and everything that happens to it - streaming arguments, an approval decision, progress and finally a result - as one message part (Kind \"tool-call\"). A session creates and drives it from the events of its transport; a page or a test can drive one by hand with the methods below. Every change raises the part's Changed event, which is what the card listens to. Thread-safe: the arguments may stream in on the transport's thread while the card reads them.",
            Parameters =
            [
                new()
                {
                    Name = "BitAiToolCallPart(toolCallId, toolName, partId?)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a call in the Streaming status, with no arguments yet. toolCallId is the provider's identifier of the call (a result is matched back to the call by it), toolName the tool being called; partId is generated when left out.",
                },
                new()
                {
                    Name = "ToolCallId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The call identifier used by the provider.",
                },
                new()
                {
                    Name = "ToolName",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The tool being called, which is also what a custom tool renderer is registered against.",
                },
                new()
                {
                    Name = "Definition",
                    Type = "BitAiToolDefinition?",
                    DefaultValue = "null",
                    Description = "The definition of the tool, resolved from the tool registry when the session starts running the call. The card is headed by its DisplayName and draws its Icon, and its Risk decides how the approval prompt looks. Until it is set, the card reads the definition registered for the tool with AddBitBlazorUIAIServices (IBitAiToolRegistry, see its table); a tool the host does not know is shown by its raw name, under the wrench.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/toolapproval#tool-definition",
                },
                new()
                {
                    Name = "Status",
                    Type = "BitAiToolCallStatus",
                    DefaultValue = "BitAiToolCallStatus.Streaming",
                    Description = "Where the call is in its lifecycle, which decides the heading, the outline and whether the card opens by itself.",
                    LinkType = LinkType.Link,
                    Href = "#tool-call-status-enum",
                },
                new()
                {
                    Name = "IsStreaming",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether the arguments are still streaming (Status is Streaming).",
                },
                new()
                {
                    Name = "RawArguments",
                    Type = "string",
                    DefaultValue = "\"\"",
                    Description = "The raw JSON argument text received so far.",
                },
                new()
                {
                    Name = "Arguments",
                    Type = "JsonElement?",
                    DefaultValue = "null",
                    Description = "The parsed arguments, available once CompleteArguments has been called.",
                },
                new()
                {
                    Name = "DisplayArguments",
                    Type = "JsonElement?",
                    DefaultValue = "null",
                    Description = "The best view of the arguments there is: the parsed ones once complete, else a lenient parse of the partial JSON received so far - which is how the card shows city: \"Os...\" while the model is still writing. Recomputed at most once per change, and only when read.",
                },
                new()
                {
                    Name = "Approval",
                    Type = "BitAiApprovalDecision",
                    DefaultValue = "BitAiApprovalDecision.Undecided",
                    Description = "The human decision on the call, when one was asked for.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/toolapproval#approval-decision-enum",
                },
                new()
                {
                    Name = "ApprovalReason",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Why the approval was asked for. The approval prompt shows it in place of its own question.",
                },
                new()
                {
                    Name = "Progress",
                    Type = "BitAiToolProgress?",
                    DefaultValue = "null",
                    Description = "The latest progress the running tool reported, drawn as a bar under the header. Cleared when the result arrives.",
                    LinkType = LinkType.Link,
                    Href = "#tool-progress",
                },
                new()
                {
                    Name = "Result",
                    Type = "BitAiToolOutcome?",
                    DefaultValue = "null",
                    Description = "The result, once the tool has produced one.",
                    LinkType = LinkType.Link,
                    Href = "#tool-outcome",
                },
                new()
                {
                    Name = "StartedAt",
                    Type = "DateTimeOffset?",
                    DefaultValue = "null",
                    Description = "When the call first moved to Running.",
                },
                new()
                {
                    Name = "CompletedAt",
                    Type = "DateTimeOffset?",
                    DefaultValue = "null",
                    Description = "When the result was recorded.",
                },
                new()
                {
                    Name = "Duration",
                    Type = "TimeSpan?",
                    DefaultValue = "null",
                    Description = "How long the call took: the Duration of its Result, else the time between StartedAt and CompletedAt. The header of a finished call shows it.",
                },
                new()
                {
                    Name = "AppendArguments(delta)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Appends a fragment of the raw JSON arguments. An empty fragment is ignored.",
                },
                new()
                {
                    Name = "CompleteArguments(arguments?)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Marks the arguments complete - the parsed value given, or a parse of the text received so far - and moves a Streaming call to Pending.",
                },
                new()
                {
                    Name = "SetStatus(status)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Moves the call to a status. Moving it to Running records StartedAt.",
                },
                new()
                {
                    Name = "SetApproval(decision)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Records a human decision: Rejected moves the call to Rejected, Approved and ApprovedAlways to Pending. A session records it through BitAiChatSession.Approve, which also resumes the turn waiting for it.",
                },
                new()
                {
                    Name = "SetProgress(progress)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Records a progress report of the running tool. A tool reports through BitAiToolInvocation.Progress, which the session forwards here.",
                },
                new()
                {
                    Name = "SetResult(outcome)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Records the result, clears the progress and moves the call to Completed or, for a failed outcome, Failed.",
                },
                new()
                {
                    Name = "PartId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier of the part (inherited from BitAiMessagePart, like the four members below): the partId given to the constructor, else a generated one. A good @key for the card.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/messagepartview#message-part",
                },
                new()
                {
                    Name = "Kind",
                    Type = "string",
                    DefaultValue = "\"tool-call\"",
                    Description = "The kind of the part (BitAiMessagePartKinds.ToolCall), which BitAiMessagePartView renders with this card.",
                },
                new()
                {
                    Name = "Version",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Increments on every change, for a cheap ShouldRender of a view of the app's own.",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage?",
                    DefaultValue = "null",
                    Description = "The message the call belongs to, once it has been added to one.",
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitAiMessagePart>",
                    DefaultValue = "",
                    Description = "Raised after every change of the call, synchronously on the thread that made it. The card subscribes to it, so it follows its call without the page re-rendering it.",
                },
            ],
        },
        new()
        {
            Id = "tool-progress",
            Title = "BitAiToolProgress",
            Description = "One progress report of a running tool (a readonly record struct), sent through BitAiToolInvocation.Progress.",
            Parameters =
            [
                new()
                {
                    Name = "Fraction",
                    Type = "double?",
                    DefaultValue = "null",
                    Description = "How much is done, from 0 to 1 (clamped). Null when the tool cannot measure it, which the bar shows by sweeping.",
                },
                new()
                {
                    Name = "Message",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A short status line, such as the current sub-step. It is the accessible name of the bar and leads the body of an open card.",
                },
            ],
        },
        new()
        {
            Id = "tool-outcome",
            Title = "BitAiToolOutcome",
            Description = "The result of one tool call: the structured Value that goes back to the model and the optional Text the card shows instead of it. Created through its factory methods.",
            Parameters =
            [
                new()
                {
                    Name = "IsSuccess",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether the call succeeded.",
                },
                new()
                {
                    Name = "Value",
                    Type = "JsonElement?",
                    DefaultValue = "null",
                    Description = "The structured result, sent back to the model. The card shows it as a JSON tree when there is no Text.",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A human-readable rendering of the result, which the card shows in place of the Value, as it was written.",
                },
                new()
                {
                    Name = "Error",
                    Type = "BitAiError?",
                    DefaultValue = "null",
                    Description = "The failure, when IsSuccess is false. The card shows it as a compact BitAiErrorView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/errorview#error",
                },
                new()
                {
                    Name = "Citations",
                    Type = "IReadOnlyList<BitAiCitation>?",
                    DefaultValue = "null",
                    Description = "The sources the tool consulted, merged into the citations of the message.",
                },
                new()
                {
                    Name = "Duration",
                    Type = "TimeSpan?",
                    DefaultValue = "null",
                    Description = "How long the call took. The session fills it in when the tool does not.",
                },
                new()
                {
                    Name = "Metadata",
                    Type = "IReadOnlyDictionary<string, object?>?",
                    DefaultValue = "null",
                    Description = "Values of the application's own, carried through to custom renderers.",
                },
                new()
                {
                    Name = "Success(value, text?)",
                    Type = "BitAiToolOutcome",
                    DefaultValue = "",
                    Description = "A successful outcome carrying a JsonElement, and optionally the text to show for it.",
                },
                new()
                {
                    Name = "Success<T>(value, typeInfo)",
                    Type = "BitAiToolOutcome",
                    DefaultValue = "",
                    Description = "A successful outcome serialized with the JsonTypeInfo<T> of the app's own JsonSerializerContext, which is safe to trim. An overload taking JsonSerializerOptions serializes by reflection instead.",
                },
                new()
                {
                    Name = "FromText(text)",
                    Type = "BitAiToolOutcome",
                    DefaultValue = "",
                    Description = "A successful outcome carrying only text (sent to the model as a JSON string).",
                },
                new()
                {
                    Name = "Failure(error)",
                    Type = "BitAiToolOutcome",
                    DefaultValue = "",
                    Description = "A failed outcome carrying a BitAiError; Failure(message) makes one of the Tool kind from a message.",
                },
                new()
                {
                    Name = "GetDisplayText()",
                    Type = "string?",
                    DefaultValue = "",
                    Description = "The text to display for the outcome: its Text, else the message of its Error, else its Value (a string as it is, anything else as JSON).",
                },
            ],
        },
        new()
        {
            Id = "tool-registry",
            Title = "IBitAiToolRegistry",
            Description = "The set of tools available to a conversation, found by the name the model calls them by (case does not matter). AddBitBlazorUIAIServices registers BitAiToolRegistry as a singleton, filled with the tools added through ai.AddTool(...) (BitAiTool.Create makes one); a session runs the tools it finds in it, and a card whose call carries no Definition yet reads the definition of its tool from it.",
            Parameters =
            [
                new()
                {
                    Name = "Tools",
                    Type = "IReadOnlyCollection<IBitAiTool>",
                    DefaultValue = "",
                    Description = "Every registered tool, each with its Definition.",
                },
                new()
                {
                    Name = "Find(name)",
                    Type = "IBitAiTool?",
                    DefaultValue = "",
                    Description = "The tool registered under that name, or null - in which case a card with no Definition on its call shows the raw name of the tool under the wrench.",
                },
                new()
                {
                    Name = "Register(tool)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a tool, or replaces the one registered under its name - at runtime too, which is how an app enables a capability for some users or conversations only.",
                },
                new()
                {
                    Name = "Unregister(name)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Removes a tool; true when one was removed.",
                },
            ],
        },
        new()
        {
            Id = "renderer-registry",
            Title = "BitAiRendererRegistry",
            Description = "Where an app registers its own renderers, which the components resolve before their built-in views. A singleton: register at startup (AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(renderers => ...))) and treat the entries as fixed afterwards. These are its tool members; the part members are on the AiMessagePartView page.",
            Parameters =
            [
                new()
                {
                    Name = "RegisterTool(toolName, renderer)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Registers a BitAiToolCallRenderer for one tool, matched by the name the model calls it by (case does not matter). Every BitAiToolCallView of that tool renders it in place of its card, inside a BitAiRendererBoundary; a later registration replaces an earlier one.",
                    LinkType = LinkType.Link,
                    Href = "#tool-call-renderer",
                },
                new()
                {
                    Name = "RegisterTool<TComponent>(toolName)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Registers a component as the renderer of one tool. The component is handed the BitAiToolCallPart as a parameter named Call.",
                },
                new()
                {
                    Name = "FindTool(toolName)",
                    Type = "BitAiToolCallRenderer?",
                    DefaultValue = "",
                    Description = "The renderer registered for a tool, or null when the built-in card draws it.",
                },
            ],
        },
        new()
        {
            Id = "tool-call-context",
            Title = "BitAiToolCallContext",
            Description = "What a custom tool renderer is handed (a sealed record).",
            Parameters =
            [
                new()
                {
                    Name = "Call",
                    Type = "BitAiToolCallPart",
                    DefaultValue = "",
                    Description = "The tool call, with its arguments, status, progress and result.",
                    LinkType = LinkType.Link,
                    Href = "#tool-call-part",
                },
                new()
                {
                    Name = "Message",
                    Type = "BitAiMessage",
                    DefaultValue = "",
                    Description = "The message the call belongs to - the card's Message, or an empty assistant message when it has none.",
                },
                new()
                {
                    Name = "Session",
                    Type = "BitAiChatSession?",
                    DefaultValue = "null",
                    Description = "The session driving the conversation, when there is one - so a renderer of its own can approve, decline or retry as well as display.",
                },
            ],
        },
        new()
        {
            Id = "tool-call-renderer",
            Title = "BitAiToolCallRenderer",
            Description = "The delegate a custom tool renderer is: public delegate RenderFragment BitAiToolCallRenderer(BitAiToolCallContext context). It is called each time the card's parameters are set, and the markup it returns is rendered again on every change of the call - so read what the markup needs off the call as it renders, rather than when the delegate runs.",
            Parameters =
            [
                new()
                {
                    Name = "context",
                    Type = "BitAiToolCallContext",
                    DefaultValue = "",
                    Description = "The call and its surroundings.",
                    LinkType = LinkType.Link,
                    Href = "#tool-call-context",
                },
                new()
                {
                    Name = "(return value)",
                    Type = "RenderFragment",
                    DefaultValue = "",
                    Description = "The markup drawn in place of the card.",
                },
            ],
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "tool-call-status-enum",
            Name = "BitAiToolCallStatus",
            Description = "Where a tool call is in its lifecycle, from the model proposing it to a result.",
            Items =
            [
                new() { Name = "Streaming", Description = "The model has named the tool and is still writing the arguments. The card is outlined as running and shows the busy dots.", Value = "0" },
                new() { Name = "Pending", Description = "The arguments are complete and the call has not started running.", Value = "1" },
                new() { Name = "AwaitingApproval", Description = "The call waits for a human to approve or decline it. The card is outlined in the warning color, opens by itself and shows the approval prompt.", Value = "2" },
                new() { Name = "Rejected", Description = "A human declined the call, so it will not run. The card is outlined in the error color.", Value = "3" },
                new() { Name = "Running", Description = "The tool is running. The card is outlined as running and shows the busy dots and the progress the tool reports.", Value = "4" },
                new() { Name = "Completed", Description = "The tool produced a result. The header shows how long it took.", Value = "5" },
                new() { Name = "Failed", Description = "The tool threw or returned a failure. The card is outlined in the error color and opens by itself.", Value = "6" },
                new() { Name = "Cancelled", Description = "The turn was stopped before the tool produced a result.", Value = "7" },
            ],
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiToolCallView-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Surface of the card." },
        new() { Name = "--bit-AiToolCallView-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Outline of the card at rest, and the rules under its header and above its approval prompt. The outline of a running, failed or waiting call wins over it." },
        new() { Name = "--bit-AiToolCallView-running-border-color", DefaultValue = "var(--bit-clr-pri)", Description = "Outline of the card of a call that is streaming its arguments or running." },
        new() { Name = "--bit-AiToolCallView-failed-border-color", DefaultValue = "var(--bit-clr-err)", Description = "Outline of the card of a call that failed or was declined." },
        new() { Name = "--bit-AiToolCallView-approval-border-color", DefaultValue = "var(--bit-clr-wrn)", Description = "Outline of the card of a call awaiting a decision." },
        new() { Name = "--bit-AiToolCallView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the card." },
        new() { Name = "--bit-AiToolCallView-header-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text and icons of the header." },
        new() { Name = "--bit-AiToolCallView-header-hover-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text and icons of the header under the pointer." },
        new() { Name = "--bit-AiToolCallView-header-hover-background", DefaultValue = "var(--bit-clr-bg-sec-hover)", Description = "Surface of the header under the pointer." },
        new() { Name = "--bit-AiToolCallView-body-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Surface of the expanded body." },
        new() { Name = "--bit-AiToolCallView-label-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Labels of the arguments and the result sections." },
    ];



    [AutoInject] private IBitAiToolRegistry toolRegistry = default!;

    private BitAiRenderScheduler sessionRenders = default!;
    private BitAiChatSession approvalSession = default!;
    private BitAiChatSession searchSession = default!;

    protected override Task OnInitAsync()
    {
        // The two sessions raise StateChanged on every event they apply, the scheduler turns those into at most one
        // render of the page per frame interval - the cards follow their own calls in between.
        sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

        approvalSession = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "delete_records",
                """{"filter":"status = 'archived'"}""",
                "Deleted 128 archived records."),
            tools: toolRegistry,
            approvalPolicy: new BitAiDefaultToolApprovalPolicy());
        approvalSession.StateChanged += sessionRenders.Request;

        searchSession = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "search_docs",
                """{"query":"render modes"}""",
                "Two passages matched."),
            tools: toolRegistry);
        searchSession.StateChanged += sessionRenders.Request;

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (approvalSession is not null) await approvalSession.DisposeAsync();
        if (searchSession is not null) await searchSession.DisposeAsync();
        if (sessionRenders is not null) await sessionRenders.DisposeAsync();

        await base.DisposeAsync(disposing);
    }



    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    private readonly BitAiToolCallPart weatherCall = CreateWeatherCall();
    private readonly BitAiUiOptions expandedOptions = BitAiUiOptions.Default with { CollapseToolCalls = false };

    private static BitAiToolCallPart CreateWeatherCall()
    {
        var call = new BitAiToolCallPart("call_weather", "get_weather")
        {
            Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
        };

        call.CompleteArguments(Json("""{"city":"Oslo"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(BitAiToolOutcome.Success(Json("""{"city":"Oslo","degrees":-3,"condition":"Snow","humidity":81}""")) with
        {
            Duration = TimeSpan.FromMilliseconds(700),
        });

        return call;
    }



    private readonly BitAiToolCallPart unknownToolCall = CreateCompletedCall("get_stock_price", null,
        """{"symbol":"MSFT"}""", "MSFT: 431.20 USD");
    private readonly BitAiToolCallPart iconToolCall = CreateCompletedCall("book_meeting",
        new BitAiToolDefinition("book_meeting") { Title = "Book a meeting", Icon = BitIconName.Calendar },
        """{"with":"Ana","on":"Tuesday 10:00"}""", "Booked with Ana for Tuesday at 10:00.");
    private readonly BitAiToolCallPart registeredToolCall = CreateCompletedCall("get_weather", null,
        """{"city":"Lisbon"}""", "Lisbon: 17 degrees, clear");

    private static BitAiToolCallPart CreateCompletedCall(string toolName, BitAiToolDefinition? definition, string arguments, string result)
    {
        var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName) { Definition = definition };

        call.CompleteArguments(Json(arguments));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(BitAiToolOutcome.FromText(result) with { Duration = TimeSpan.FromMilliseconds(400) });

        return call;
    }



    private BitAiToolCallPart? indexCall;
    private bool isIndexing;

    private async Task RunIndexAsync()
    {
        isIndexing = true;
        indexCall = new BitAiToolCallPart(BitAiId.NewToolCallId(), "index_docs")
        {
            Definition = new BitAiToolDefinition("index_docs") { Title = "Index the docs" },
        };
        StateHasChanged();

        // The model writes the arguments a few characters at a time; the card shows them as they arrive.
        foreach (var slice in new[] { """{"fol""", """der":"do""", """cs/","rec""", """ursive":""", "true}" })
        {
            await Task.Delay(250);
            indexCall.AppendArguments(slice);
        }

        indexCall.CompleteArguments();

        await Task.Delay(400);
        indexCall.SetStatus(BitAiToolCallStatus.Running);
        indexCall.SetProgress(new BitAiToolProgress(null, "Listing the files"));

        await Task.Delay(1200);
        indexCall.SetProgress(new BitAiToolProgress(0.4, "Reading 12 of 30 files"));

        await Task.Delay(1200);
        indexCall.SetProgress(new BitAiToolProgress(0.8, "Reading 24 of 30 files"));

        await Task.Delay(1200);
        indexCall.SetResult(BitAiToolOutcome.FromText("Indexed 30 files of docs/ (412 passages)."));

        isIndexing = false;
    }



    private readonly BitAiToolCallPart failedCall = CreateFailedCall();
    private readonly BitAiToolCallPart declinedCall = CreateDeclinedCall();

    private static BitAiToolCallPart CreateFailedCall()
    {
        var call = new BitAiToolCallPart("call_atlantis", "get_weather")
        {
            Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" },
        };

        call.CompleteArguments(Json("""{"city":"Atlantis"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(BitAiToolOutcome.Failure(new BitAiError("No weather station reports for Atlantis.")
        {
            Code = "unknown_city",
            Kind = BitAiErrorKind.Tool,
        }) with
        {
            Duration = TimeSpan.FromMilliseconds(1200),
        });

        return call;
    }

    private static BitAiToolCallPart CreateDeclinedCall()
    {
        var call = new BitAiToolCallPart("call_delete", "delete_records")
        {
            Definition = new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive },
        };

        call.CompleteArguments(Json("""{"filter":"status = 'archived'"}"""));
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);
        call.SetApproval(BitAiApprovalDecision.Rejected);
        call.SetResult(BitAiToolOutcome.Failure(new BitAiError("The user declined this tool call.")
        {
            Code = "rejected",
            Kind = BitAiErrorKind.Tool,
        }));
        call.SetStatus(BitAiToolCallStatus.Rejected);

        return call;
    }

    private readonly BitAiToolCallPart cancelledCall = CreateCancelledCall();

    private static BitAiToolCallPart CreateCancelledCall()
    {
        var call = new BitAiToolCallPart("call_export", "export_report")
        {
            Definition = new BitAiToolDefinition("export_report") { Title = "Export the report" },
        };

        call.CompleteArguments(Json("""{"format":"pdf","pages":"all"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetStatus(BitAiToolCallStatus.Cancelled);

        return call;
    }



    private Task AskToDeleteAsync() => approvalSession.SubmitAsync("Delete the archived records");



    private Task SearchDocsAsync() => searchSession.SubmitAsync("Find the docs on render modes");



    private readonly BitAiToolCallPart awaitingCall = CreateAwaitingCall();

    private static BitAiToolCallPart CreateAwaitingCall()
    {
        var call = new BitAiToolCallPart("call_email", "send_email")
        {
            Definition = new BitAiToolDefinition("send_email") { Title = "Send email", Risk = BitAiToolRisk.Write, RequiresApproval = true },
            ApprovalReason = "Send the summary to the 3 people on the thread?",
        };

        call.CompleteArguments(Json("""{"to":["ana@example.com","li@example.com","sam@example.com"],"subject":"Weekly summary"}"""));
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        return call;
    }



    private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
    private readonly BitAiToolCallPart rtlCall = CreateRtlCall(succeeded: true);
    private readonly BitAiToolCallPart rtlFailedCall = CreateRtlCall(succeeded: false);

    private static BitAiToolCallPart CreateRtlCall(bool succeeded)
    {
        var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), "get_weather")
        {
            Definition = new BitAiToolDefinition("get_weather") { Title = "آب و هوا" },
        };

        call.CompleteArguments(Json(succeeded ? """{"city":"تهران"}""" : """{"city":"آتلانتیس"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(succeeded
            ? BitAiToolOutcome.FromText("تهران: ۱۸ درجه، آفتابی") with { Duration = TimeSpan.FromMilliseconds(650) }
            : BitAiToolOutcome.Failure("هیچ ایستگاه هواشناسی‌ای برای آتلانتیس گزارشی ندارد.") with { Duration = TimeSpan.FromMilliseconds(1200) });

        return call;
    }

    private sealed class PersianAiStrings : BitAiStrings
    {
        public override string CallingTool(string tool) => $"در حال اجرای {tool}";
        public override string CalledTool(string tool) => $"{tool} اجرا شد";
        public override string ToolFailed(string tool) => $"{tool} ناموفق بود";
        public override string ToolCancelled(string tool) => $"{tool} لغو شد";
        public override string ToolRejected(string tool) => $"{tool} رد شد";
        public override string ApproveTool(string tool) => $"اجازه‌ی اجرای {tool} داده شود؟";
        public override string ToolAwaitingApproval(string tool) => $"{tool} منتظر اجازه‌ی شماست";
        public override string Approve => "اجازه";
        public override string ApproveAlways => "همیشه اجازه";
        public override string Reject => "رد";
        public override string Arguments => "ورودی‌ها";
        public override string Result => "نتیجه";
        public override string FormatDuration(TimeSpan duration) => $"{duration.TotalSeconds:F1} ثانیه";
    }



    private readonly BitAiToolCallViewParams[] toolCallViewParams =
    [
        new() { Disabled = true, Styles = new() { Root = "border-style: dashed", Title = "font-style: italic" } },
    ];
}
