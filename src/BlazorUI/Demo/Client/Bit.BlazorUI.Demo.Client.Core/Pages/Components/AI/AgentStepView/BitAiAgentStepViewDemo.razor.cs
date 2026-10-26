namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AgentStepView;

public partial class BitAiAgentStepViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiAgentStepViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the agent step view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Conversation",
            Type = "BitAiConversation?",
            DefaultValue = "null",
            Description = "The conversation the run belongs to, in which the tool calls of the step are looked up (newest message first) to show them by tool name and status: a running call with the dots of a loader, a failed one in the error color. Without it the calls are listed by their identifiers. A BitAiAgentRunView hands down the conversation of its Session.",
            LinkType = LinkType.Link,
            Href = "/components/ai/messagelist#conversation",
        },
        new()
        {
            Name = "Step",
            Type = "BitAiAgentStep",
            DefaultValue = "",
            Description = "The step to render (required). The view subscribes to its Changed event and re-renders at most once per render interval as it changes; a tool call changing on its own is drawn with the next change of the step.",
            LinkType = LinkType.Link,
            Href = "#agent-step",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiAgentStepViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the agent step view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiAgentStepViewClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiAgentStepView." },
                new() { Name = "Marker", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the timeline marker of the BitAiAgentStepView." },
                new() { Name = "MarkerIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the check icon of the marker of a completed BitAiAgentStepView." },
                new() { Name = "Body", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the body (everything beside the marker) of the BitAiAgentStepView." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title row (the title and the duration) of the BitAiAgentStepView." },
                new() { Name = "Duration", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the duration (or the time elapsed so far) of the BitAiAgentStepView." },
                new() { Name = "Detail", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the detail of the BitAiAgentStepView." },
                new() { Name = "Summary", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the summary of the BitAiAgentStepView." },
                new() { Name = "ToolCalls", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list of tool calls of the BitAiAgentStepView." },
                new() { Name = "ToolCall", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the list of tool calls of the BitAiAgentStepView." },
                new()
                {
                    Name = "ToolCallTag",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag of each tool call of the BitAiAgentStepView.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag#tag-class-styles",
                },
                new() { Name = "Error", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error message of the BitAiAgentStepView." },
                new()
                {
                    Name = "ErrorDetailButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button (a BitButton) disclosing the detail of the error of a failed BitAiAgentStepView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
                new() { Name = "ErrorDetail", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the detail of the error of a failed BitAiAgentStepView, shown behind the disclosure." },
            ]
        },
        new()
        {
            Id = "agent-step",
            Title = "BitAiAgentStep",
            Description = "One step of an agent run - the unit the user reasons about (\"search the docs\"), which usually contains one or more tool calls. A run creates its steps from its plan (BitAiAgentRun.SetPlan, GetOrAddStep); an application keeping steps of its own creates one with new BitAiAgentStep(stepId, title). Every change raises Changed.",
            Parameters =
            [
                new() { Name = "StepId", Type = "string", DefaultValue = "", Description = "The stable identifier of the step, preserved across plan revisions." },
                new() { Name = "Title", Type = "string", DefaultValue = "", Description = "What the step does. Set by the constructor and renewed by a plan revision." },
                new() { Name = "Detail", Type = "string?", DefaultValue = "null", Description = "An elaboration of the step, shown under its title." },
                new()
                {
                    Name = "Status",
                    Type = "BitAiAgentStepStatus",
                    DefaultValue = "BitAiAgentStepStatus.Pending",
                    Description = "Where the step is in its lifecycle: moved by Start, Block and Complete.",
                    LinkType = LinkType.Link,
                    Href = "#agent-step-status-enum",
                },
                new() { Name = "Summary", Type = "string?", DefaultValue = "null", Description = "A short account of what the step produced, given to Complete." },
                new()
                {
                    Name = "Error",
                    Type = "BitAiError?",
                    DefaultValue = "null",
                    Description = "The failure, when the step failed: its Message is shown under the step, and its Detail behind a Show details disclosure.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/errorview#error",
                },
                new() { Name = "StartedAt", Type = "DateTimeOffset?", DefaultValue = "null", Description = "When the step started (the first Start)." },
                new() { Name = "CompletedAt", Type = "DateTimeOffset?", DefaultValue = "null", Description = "When the step finished." },
                new() { Name = "Duration", Type = "TimeSpan?", DefaultValue = "null", Description = "How long the step took, once it has both started and finished; a step completed without being started has none, so the view shows no time for it." },
                new() { Name = "ToolCallIds", Type = "IReadOnlyList<string>", DefaultValue = "[]", Description = "The identifiers of the tool calls made during this step." },
                new() { Name = "Version", Type = "int", DefaultValue = "0", Description = "Increments on every change, for cheap render checks." },
                new() { Name = "Changed", Type = "event Action<BitAiAgentStep>?", DefaultValue = "", Description = "Raised after the step changes." },
                new() { Name = "Start", Type = "void", DefaultValue = "", Description = "Start(): marks the step running, and records when it started the first time." },
                new() { Name = "Block", Type = "void", DefaultValue = "", Description = "Block(): marks the step blocked on the user (a question, an approval)." },
                new() { Name = "AddToolCall", Type = "void", DefaultValue = "", Description = "AddToolCall(string toolCallId): records a tool call made during this step. Recording the same call twice has no effect, and calls reported in parallel are all kept." },
                new()
                {
                    Name = "Complete",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Complete(BitAiAgentStepStatus status = Completed, string? summary = null, BitAiError? error = null): marks the step finished - Completed, Failed or Skipped - with what it produced or what it failed with, and records when it finished.",
                    LinkType = LinkType.Link,
                    Href = "#agent-step-status-enum",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "agent-step-status-enum",
            Name = "BitAiAgentStepStatus",
            Description = "The lifecycle of one step within an agent run, which the marker of a BitAiAgentStepView draws.",
            Items =
            [
                new() { Name = "Pending", Description = "Planned but not started. Its marker is an empty ring and its title is dimmed.", Value = "0" },
                new() { Name = "Running", Description = "Currently executing. Its marker pulses, and the time it has been running shows beside the title.", Value = "1" },
                new() { Name = "Blocked", Description = "Paused pending human input or approval: a question of its run (BitAiAgentRun.RequestInput) blocks the steps running with it until it is answered. Its marker is drawn in the warning color.", Value = "2" },
                new() { Name = "Completed", Description = "Finished successfully. Its marker carries a check mark, and the time it took shows beside the title.", Value = "3" },
                new() { Name = "Failed", Description = "Finished unsuccessfully. Its marker is drawn in the error color, and the message of its error shows under it, with the detail of the error behind a disclosure.", Value = "4" },
                new() { Name = "Skipped", Description = "Abandoned because the run ended or the plan changed: a run that fails or is cancelled skips every step it did not finish. Drawn like a pending step.", Value = "5" },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiAgentStepView-gap", DefaultValue = "spacing(1.5)", Description = "Room between the marker and the body." },
        new() { Name = "--bit-AiAgentStepView-marker-size", DefaultValue = "var(--bit-siz-icon-md)", Description = "Diameter of the timeline marker, which the line to the next step is placed by too." },
        new() { Name = "--bit-AiAgentStepView-marker-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Ring of a pending or skipped step." },
        new() { Name = "--bit-AiAgentStepView-running-color", DefaultValue = "var(--bit-clr-pri)", Description = "Ring and pulse of the running step." },
        new() { Name = "--bit-AiAgentStepView-completed-color", DefaultValue = "var(--bit-clr-suc)", Description = "Marker of a completed step." },
        new() { Name = "--bit-AiAgentStepView-completed-icon-color", DefaultValue = "var(--bit-clr-suc-text)", Description = "Check mark of a completed step." },
        new() { Name = "--bit-AiAgentStepView-failed-color", DefaultValue = "var(--bit-clr-err)", Description = "Marker of a failed step." },
        new() { Name = "--bit-AiAgentStepView-blocked-color", DefaultValue = "var(--bit-clr-wrn)", Description = "Marker of a step waiting on the user." },
        new() { Name = "--bit-AiAgentStepView-line-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Line joining a step to the next one." },
        new() { Name = "--bit-AiAgentStepView-title-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the title (the rest of the step is sized from it)." },
        new() { Name = "--bit-AiAgentStepView-text-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The detail, the summary and the duration, and the detail of the error of a failed step." },
        new() { Name = "--bit-AiAgentStepView-error-color", DefaultValue = "var(--bit-clr-err-fg)", Description = "The error message of a failed step." },
    ];



    // Basic: a step the page runs itself.
    private BitAiAgentStep basicStep = new("search", "Search the documentation");
    private bool isBasicRunning;

    private async Task RunBasicStep()
    {
        isBasicRunning = true;

        var step = new BitAiAgentStep("search", "Search the documentation");
        basicStep = step;

        step.Start();
        await Task.Delay(1500);
        step.Complete(summary: "2 passages matched");

        isBasicRunning = false;
    }



    // Statuses: one step of every status.
    private readonly BitAiAgentStep[] statusSteps = CreateStatusSteps();

    private static BitAiAgentStep[] CreateStatusSteps()
    {
        var pending = new BitAiAgentStep("pending", "Pending: planned, not started yet");

        var running = new BitAiAgentStep("running", "Running: being worked on");
        running.Start();

        var blocked = new BitAiAgentStep("blocked", "Blocked: waiting on the user");
        blocked.Start();
        blocked.Block();

        var completed = new BitAiAgentStep("completed", "Completed: finished successfully");
        completed.Complete(summary: "11 results, 3 look primary");

        var failed = new BitAiAgentStep("failed", "Failed: finished unsuccessfully");
        failed.Complete(BitAiAgentStepStatus.Failed, error: new BitAiError("3 of 812 tests failed.")
        {
            Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund",
        });

        var skipped = new BitAiAgentStep("skipped", "Skipped: abandoned when the run ended");
        skipped.Complete(BitAiAgentStepStatus.Skipped);

        return [pending, running, blocked, completed, failed, skipped];
    }



    // Detail & summary.
    private readonly BitAiAgentStep detailStep = CreateDetailStep();

    private static BitAiAgentStep CreateDetailStep()
    {
        var step = new BitAiAgentStep("reconcile", "Reconcile the disagreements")
        {
            Detail = "Compare how each study chose its population, and whether that explains the gap.",
        };

        step.Complete(summary: "The disagreement is about scope, not direction");

        return step;
    }



    // Tool calls: the calls of a step, looked up in the conversation they belong to.
    private BitAiConversation toolConversation = new();
    private BitAiAgentStep toolStep = new("forecast", "Look up the forecast and the travel advice");
    private bool isToolRunning;

    private async Task RunToolStep()
    {
        isToolRunning = true;

        var conversation = new BitAiConversation();
        var message = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
        var step = new BitAiAgentStep("forecast", "Look up the forecast and the travel advice");
        toolConversation = conversation;
        toolStep = step;

        step.Start();

        var weather = message.AddPart(new BitAiToolCallPart(BitAiId.NewToolCallId(), "get_weather"));
        weather.SetStatus(BitAiToolCallStatus.Running);
        step.AddToolCall(weather.ToolCallId);
        await Task.Delay(1500);
        weather.SetResult(BitAiToolOutcome.FromText("Oslo: -3 degrees, snow"));

        var advice = message.AddPart(new BitAiToolCallPart(BitAiId.NewToolCallId(), "search_docs"));
        advice.SetStatus(BitAiToolCallStatus.Running);
        step.AddToolCall(advice.ToolCallId);
        await Task.Delay(1500);
        advice.SetResult(BitAiToolOutcome.Failure("The travel advice index is unavailable."));

        step.Complete(summary: "The forecast is in; the travel advice could not be read");

        isToolRunning = false;
    }



    // Cascading parameters.
    private readonly BitAiAgentStepViewParams[] stepViewParams =
    [
        new() { Styles = new() { Summary = "font-style: italic", Title = "color: var(--bit-clr-pri)" } }
    ];
    private readonly BitAiAgentStep[] cascadingSteps = CreateCascadingSteps();

    private static BitAiAgentStep[] CreateCascadingSteps()
    {
        var outline = new BitAiAgentStep("outline", "Outline the post");
        outline.Complete(summary: "Five sections");

        var draft = new BitAiAgentStep("draft", "Draft it");
        draft.Complete(summary: "1,200 words");

        var proofread = new BitAiAgentStep("proofread", "Proofread it");
        proofread.Complete(summary: "Two typos fixed");

        return [outline, draft, proofread];
    }



    // Style & Class.
    private readonly BitAiAgentStep[] styleSteps = CreateStyleSteps();

    private static BitAiAgentStep[] CreateStyleSteps()
    {
        var venue = new BitAiAgentStep("venue", "Book the venue");
        venue.Complete(summary: "The lakeside hall, for 40");

        var invitations = new BitAiAgentStep("invitations", "Send the invitations");
        invitations.Start();

        return [venue, invitations];
    }



    // RTL.
    private readonly BitAiAgentStep[] rtlSteps = CreateRtlSteps();

    private static BitAiAgentStep[] CreateRtlSteps()
    {
        var search = new BitAiAgentStep("search", "جستجوی منابع اصلی");
        search.Complete(summary: "۱۱ نتیجه، ۳ تا اصلی به نظر می‌رسند");

        var read = new BitAiAgentStep("read", "خواندن سه منبع مرتبط‌تر");
        read.Start();

        var write = new BitAiAgentStep("write", "نوشتن خلاصه");

        return [search, read, write];
    }
}
