namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AgentRunView;

public partial class BitAiAgentRunViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiAgentRunViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the agent run view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "ClockInterval",
            Type = "TimeSpan",
            DefaultValue = "TimeSpan.FromSeconds(1)",
            Description = "How often the elapsed time is refreshed while the run is active. The clock starts with the run and stops the moment it ends (completed, failed or cancelled), so a finished run costs nothing to keep on screen. Each tick is one coalesced render of the view.",
        },
        new()
        {
            Name = "OnRespond",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback for when the user answers the question the run is blocked on - a suggested answer or one typed into the field (Enter or the Answer button). When set, the answer is handed to the callback alone and the Session is not called, so one answer is never delivered twice; the callback is then what resolves the run (ResolveInput) and carries it on. The answer buttons are disabled while it runs.",
        },
        new()
        {
            Name = "Run",
            Type = "BitAiAgentRun",
            DefaultValue = "",
            Description = "The run to render (required). The view subscribes to its Changed event, and re-renders at most once per render interval as it changes, so the run can be moved from any thread without calling StateHasChanged.",
            LinkType = LinkType.Link,
            Href = "#agent-run",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session the run belongs to. Without an OnRespond, an answer is handed to its RespondToAgentAsync, which resolves the run, appends the answer as a user message and continues the turn. Its conversation also lets each step show the tool calls it made by name and status.",
            LinkType = LinkType.Link,
            Href = "/components/ai/chat#chat-session",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiAgentRunViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the agent run view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiAgentRunViewClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiAgentRunView." },
                new() { Name = "Header", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header of the BitAiAgentRunView." },
                new() { Name = "HeaderIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the header of the BitAiAgentRunView." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title (the name of the agent, or \"Plan\") of the BitAiAgentRunView." },
                new()
                {
                    Name = "Usage",
                    Type = "BitAiTokenUsageViewClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiTokenUsageView of the header of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/tokenusageview#class-styles",
                },
                new()
                {
                    Name = "StatusBadge",
                    Type = "BitAiAgentStatusBadgeClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiAgentStatusBadge of the header of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentstatusbadge#class-styles",
                },
                new() { Name = "Body", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the body of the BitAiAgentRunView." },
                new() { Name = "Goal", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the goal of the BitAiAgentRunView." },
                new() { Name = "Progress", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the progress row (the count, the bar and the clock) of the BitAiAgentRunView." },
                new() { Name = "Count", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the count of the completed steps of the BitAiAgentRunView." },
                new()
                {
                    Name = "ProgressBar",
                    Type = "BitProgressClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitProgress of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/progress#progressBar-class-styles",
                },
                new() { Name = "Elapsed", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the elapsed time of the BitAiAgentRunView." },
                new() { Name = "Steps", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list of the steps of the BitAiAgentRunView." },
                new() { Name = "StepItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each item of the list of the steps of the BitAiAgentRunView." },
                new()
                {
                    Name = "Step",
                    Type = "BitAiAgentStepViewClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiAgentStepView of each step of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentstepview#class-styles",
                },
                new() { Name = "Prompt", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the prompt (the question the run asks the user, its suggested answers and the answer row) of the BitAiAgentRunView." },
                new() { Name = "Question", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text of the question of the BitAiAgentRunView." },
                new()
                {
                    Name = "SuggestionButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitButton of each suggested answer of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
                new() { Name = "Answer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the answer row (the field and its button) of the BitAiAgentRunView." },
                new()
                {
                    Name = "AnswerField",
                    Type = "BitTextFieldClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField of the answer of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/textfield#textfield-class-styles",
                },
                new()
                {
                    Name = "AnswerButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitButton sending the answer of the BitAiAgentRunView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles",
                },
                new() { Name = "Error", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error (the wrapper of the compact BitAiErrorView of a failed run) of the BitAiAgentRunView." },
            ]
        },
        new()
        {
            Id = "agent-run",
            Title = "BitAiAgentRun",
            Description = "A multi-step agent execution: a plan, the steps as they run, and the aggregate result. A BitAiChatSession builds one from the agent events of its transport and keeps it in Conversation.Runs; an application driving an agent of its own creates one with new BitAiAgentRun(runId, agentName) - both optional, a run id is generated when none is given - and moves it with the methods below. Every change raises Changed. Thread-safe to read while it is moved from another thread.",
            Parameters =
            [
                new() { Name = "RunId", Type = "string", DefaultValue = "", Description = "The run identifier - the one the agent events address the run by." },
                new() { Name = "AgentName", Type = "string?", DefaultValue = "null", Description = "The name of the agent, the title of the run view. The view reads \"Plan\" (BitAiStrings.Plan) without one." },
                new() { Name = "Goal", Type = "string?", DefaultValue = "null", Description = "The goal the run is pursuing, shown above the steps." },
                new()
                {
                    Name = "Status",
                    Type = "BitAiAgentRunStatus",
                    DefaultValue = "BitAiAgentRunStatus.Idle",
                    Description = "Where the run is in its lifecycle. Moved by SetPlan (Idle to Planning), SetStatus, RequestInput (WaitingForInput), ResolveInput (back to Running) and SetError (Failed or Cancelled).",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentstatusbadge#agent-run-status-enum",
                },
                new()
                {
                    Name = "Steps",
                    Type = "IReadOnlyList<BitAiAgentStep>",
                    DefaultValue = "[]",
                    Description = "The steps, in execution order.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentstepview#agent-step",
                },
                new()
                {
                    Name = "CurrentStep",
                    Type = "BitAiAgentStep?",
                    DefaultValue = "null",
                    Description = "The step currently running, else the one blocked on the user, else null.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/agentstepview#agent-step",
                },
                new() { Name = "PendingPrompt", Type = "string?", DefaultValue = "null", Description = "What the agent is waiting for, while the run is blocked on the user." },
                new() { Name = "PendingSuggestions", Type = "IReadOnlyList<string>?", DefaultValue = "null", Description = "The suggested answers to PendingPrompt, rendered as one-tap choices." },
                new() { Name = "StartedAt", Type = "DateTimeOffset", DefaultValue = "", Description = "When the run started (when it was created)." },
                new() { Name = "CompletedAt", Type = "DateTimeOffset?", DefaultValue = "null", Description = "When the run ended: set the first time it moves to Completed, Failed or Cancelled." },
                new() { Name = "Elapsed", Type = "TimeSpan", DefaultValue = "", Description = "How long the run took, or has taken so far." },
                new()
                {
                    Name = "Usage",
                    Type = "BitAiTokenUsage",
                    DefaultValue = "",
                    Description = "The token usage of every model request made within the run, each counted once with the last usage its stream reported. Shown in the header of the view when BitAiUiOptions.ShowUsage is on.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/tokenusageview#token-usage",
                },
                new()
                {
                    Name = "Error",
                    Type = "BitAiError?",
                    DefaultValue = "null",
                    Description = "The failure that ended the run, when it failed. The view shows it under the steps as a compact BitAiErrorView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/errorview#error",
                },
                new() { Name = "CompletedStepCount", Type = "int", DefaultValue = "0", Description = "How many steps have finished successfully." },
                new() { Name = "Progress", Type = "double?", DefaultValue = "null", Description = "Completion in the range 0..1 (the completed steps over all of them), or null while there is no plan - in which case the view shows no progress row." },
                new() { Name = "Version", Type = "int", DefaultValue = "0", Description = "Increments on every change, for cheap render checks." },
                new() { Name = "Changed", Type = "event Action<BitAiAgentRun>?", DefaultValue = "", Description = "Raised after the run (or any of its steps) changes." },
                new()
                {
                    Name = "SetPlan",
                    Type = "void",
                    DefaultValue = "",
                    Description = "SetPlan(IReadOnlyList<BitAiAgentPlanStep> steps): replaces the plan. A step whose StepId survives the revision keeps its state and timings (its title and detail are updated); a repeated id counts once. Moves an Idle run to Planning.",
                    LinkType = LinkType.Link,
                    Href = "#agent-plan-step",
                },
                new() { Name = "GetOrAddStep", Type = "BitAiAgentStep", DefaultValue = "", Description = "GetOrAddStep(string stepId, string? title = null): finds a step, creating it at the end when the agent starts one that was never planned. A title given renames the step." },
                new() { Name = "SetStatus", Type = "void", DefaultValue = "", Description = "SetStatus(BitAiAgentRunStatus status): moves the run to a new status. Completed, Failed and Cancelled end it: CompletedAt is set and any pending question is cleared." },
                new() { Name = "RequestInput", Type = "void", DefaultValue = "", Description = "RequestInput(string prompt, IReadOnlyList<string>? suggestions = null): blocks the run on the user until they answer; the run moves to WaitingForInput." },
                new() { Name = "ResolveInput", Type = "void", DefaultValue = "", Description = "ResolveInput(): clears the pending question and moves a waiting run back to Running." },
                new() { Name = "AddUsage", Type = "void", DefaultValue = "", Description = "AddUsage(BitAiTokenUsage usage): adds the final token usage of one request to the run total." },
                new() { Name = "SetError", Type = "void", DefaultValue = "", Description = "SetError(BitAiError error): records the failure that ended the run, and ends it as Failed - or Cancelled, for an error whose Kind is Cancelled." },
            ]
        },
        new()
        {
            Id = "agent-plan-step",
            Title = "BitAiAgentPlanStep",
            Description = "One entry of an agent plan, handed to BitAiAgentRun.SetPlan or carried by a BitAiAgentPlanEvent: new BitAiAgentPlanStep(stepId, title, detail). A readonly record struct.",
            Parameters =
            [
                new() { Name = "StepId", Type = "string", DefaultValue = "", Description = "The stable identifier of the step, preserved across plan revisions." },
                new() { Name = "Title", Type = "string", DefaultValue = "", Description = "What the step does." },
                new() { Name = "Detail", Type = "string?", DefaultValue = "null", Description = "An optional elaboration, shown under the title of the step." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiAgentRunView-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Surface of the panel and its header." },
        new() { Name = "--bit-AiAgentRunView-body-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Surface of the body under the header." },
        new() { Name = "--bit-AiAgentRunView-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Edge of the panel, and the line under its header." },
        new() { Name = "--bit-AiAgentRunView-border-width", DefaultValue = "var(--bit-shp-brd-width)", Description = "Width of that edge and line." },
        new() { Name = "--bit-AiAgentRunView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner of the panel." },
        new() { Name = "--bit-AiAgentRunView-header-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Icon and words of the header." },
        new() { Name = "--bit-AiAgentRunView-text-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The goal, the count of the steps and the clock." },
        new() { Name = "--bit-AiAgentRunView-prompt-background", DefaultValue = "var(--bit-clr-wrn-tint)", Description = "Surface of the question the run asks the user." },
        new() { Name = "--bit-AiAgentRunView-progress-max-width", DefaultValue = "spacing(24)", Description = "Width the progress bar stops growing at." },
    ];



    // Basic: a run the page drives itself, step by step.
    private BitAiAgentRun? basicRun;
    private bool isBasicRunning;

    private async Task StartBasicRun()
    {
        isBasicRunning = true;

        var run = new BitAiAgentRun(agentName: "Research agent") { Goal = "Find out why the three studies disagree" };
        basicRun = run;

        run.SetPlan(
        [
            new BitAiAgentPlanStep("search", "Search for primary sources"),
            new BitAiAgentPlanStep("read", "Read the three most relevant"),
            new BitAiAgentPlanStep("reconcile", "Reconcile the disagreements"),
            new BitAiAgentPlanStep("write", "Write the summary"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        string[] summaries =
        [
            "11 results, 3 look primary",
            "Two agree on the mechanism, one does not",
            "The disagreement is about scope, not direction",
            "Summary written",
        ];

        for (var i = 0; i < run.Steps.Count; i++)
        {
            run.Steps[i].Start();
            await Task.Delay(1200);
            run.Steps[i].Complete(summary: summaries[i]);
        }

        run.SetStatus(BitAiAgentRunStatus.Completed);
        isBasicRunning = false;
    }



    // Failure: a failed step, the skipped rest, and the error of the run.
    private BitAiAgentRun? failingRun;
    private bool isFailingRunning;

    private async Task StartFailingRun()
    {
        isFailingRunning = true;

        var run = new BitAiAgentRun(agentName: "Deploy agent") { Goal = "Ship the release" };
        failingRun = run;

        run.SetPlan(
        [
            new BitAiAgentPlanStep("build", "Build"),
            new BitAiAgentPlanStep("test", "Run the test suite"),
            new BitAiAgentPlanStep("deploy", "Deploy"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        var build = run.Steps[0];
        build.Start();
        await Task.Delay(1000);
        build.Complete(summary: "Build succeeded in 41s");

        var test = run.Steps[1];
        test.Start();
        await Task.Delay(1500);
        test.Complete(BitAiAgentStepStatus.Failed, error: new BitAiError("3 of 812 tests failed."));

        run.Steps[2].Complete(BitAiAgentStepStatus.Skipped);

        run.SetError(new BitAiError("The run stopped before deploying.")
        {
            Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund",
        });

        isFailingRunning = false;
    }



    // Asking the user: a run blocked on a question, resumed by OnRespond.
    private BitAiAgentRun? askingRun;
    private bool isAskingRunning;

    private async Task StartAskingRun()
    {
        isAskingRunning = true;

        var run = new BitAiAgentRun(agentName: "Migration agent") { Goal = "Move the database to the new schema" };
        askingRun = run;

        run.SetPlan(
        [
            new BitAiAgentPlanStep("inspect", "Inspect the current schema"),
            new BitAiAgentPlanStep("window", "Choose a downtime window"),
            new BitAiAgentPlanStep("migrate", "Run the migration"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        var inspect = run.Steps[0];
        inspect.Start();
        await Task.Delay(1000);
        inspect.Complete(summary: "14 tables, 2 need rewriting");

        var window = run.Steps[1];
        window.Start();
        window.Block();
        run.RequestInput("Two tables need a rewrite, which needs downtime. When should I take it?",
                         ["Tonight after 22:00", "Saturday morning"]);

        isAskingRunning = false;
    }

    private async Task RespondToAskingRun(string answer)
    {
        var run = askingRun!;

        run.ResolveInput();
        run.Steps[1].Complete(summary: $"Window agreed: {answer}");

        var migrate = run.Steps[2];
        migrate.Start();
        await Task.Delay(1200);
        migrate.Complete(summary: "14 tables migrated");

        run.SetStatus(BitAiAgentRunStatus.Completed);
    }



    // With a session: the run a session builds from the agent events of its transport.
    private BitAiChatSession? session;
    private BitAiAgentRun? sessionRun;

    private async Task StartSessionRun(IBitAiTransport transport)
    {
        if (session is not null) await session.DisposeAsync();

        var current = new BitAiChatSession(transport);
        session = current;
        sessionRun = null;

        // The run appears in the conversation once the transport starts it; from then on the view follows it.
        current.StateChanged += () =>
        {
            var run = current.Conversation.Runs.Values.FirstOrDefault();
            if (ReferenceEquals(run, sessionRun) || ReferenceEquals(current, session) is false) return;

            sessionRun = run;
            InvokeAsync(StateHasChanged);
        };

        await current.SubmitAsync("Find out why the three studies disagree");
    }



    // Usage & clock: the total usage in the header, and a clock moving ten times a second.
    private readonly BitAiUiOptions usageOptions = BitAiUiOptions.Default with { ShowUsage = true };
    private BitAiAgentRun? usageRun;
    private bool isUsageRunning;

    private async Task StartUsageRun()
    {
        isUsageRunning = true;

        var run = new BitAiAgentRun(agentName: "Research agent") { Goal = "Summarize the release notes" };
        usageRun = run;

        run.SetPlan(
        [
            new BitAiAgentPlanStep("fetch", "Fetch the release notes"),
            new BitAiAgentPlanStep("summarize", "Summarize them"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        foreach (var step in run.Steps)
        {
            step.Start();
            await Task.Delay(1500);

            // One model request per step, priced by the transport.
            run.AddUsage(new BitAiTokenUsage { InputTokens = 1_850, OutputTokens = 420, Cost = 0.0119m, Currency = "USD" });
            step.Complete();
        }

        run.SetStatus(BitAiAgentRunStatus.Completed);
        isUsageRunning = false;
    }



    // Cascading parameters: the defaults of every run view inside a BitParams.
    private readonly BitAiAgentRunViewParams[] runViewParams =
    [
        new()
        {
            ClockInterval = TimeSpan.FromMilliseconds(250),
            Styles = new() { Goal = "font-style: italic", Title = "color: var(--bit-clr-pri)" },
        }
    ];
    private BitAiAgentRun? cascadingRun;
    private bool isCascadingRunning;

    private async Task StartCascadingRun()
    {
        isCascadingRunning = true;

        var run = new BitAiAgentRun(agentName: "Writer") { Goal = "Draft the announcement" };
        cascadingRun = run;

        run.SetPlan(
        [
            new BitAiAgentPlanStep("outline", "Outline the post"),
            new BitAiAgentPlanStep("draft", "Draft it"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        foreach (var step in run.Steps)
        {
            step.Start();
            await Task.Delay(1200);
            step.Complete();
        }

        run.SetStatus(BitAiAgentRunStatus.Completed);
        isCascadingRunning = false;
    }



    // Style & Class: one run, waiting on the user, drawn by three views.
    private BitAiAgentRun styledRun = CreateStyledRun();

    private static BitAiAgentRun CreateStyledRun()
    {
        var run = new BitAiAgentRun(agentName: "Travel agent") { Goal = "Book the team offsite" };

        run.SetPlan(
        [
            new BitAiAgentPlanStep("dates", "Agree on the dates"),
            new BitAiAgentPlanStep("venue", "Book the venue"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        run.Steps[0].Start();
        run.Steps[0].Block();
        run.RequestInput("Which week suits the team?", ["The first week of June", "The last week of June"]);

        return run;
    }

    private void RespondToStyledRun(string answer)
    {
        styledRun.ResolveInput();
        styledRun.Steps[0].Complete(summary: answer);
        styledRun.Steps[1].Start();
    }

    private void ResetStyledRun() => styledRun = CreateStyledRun();



    // RTL: the same, in Persian.
    private BitAiAgentRun rtlRun = CreateRtlRun();

    private static BitAiAgentRun CreateRtlRun()
    {
        var run = new BitAiAgentRun(agentName: "دستیار سفر") { Goal = "رزرو سفر کاری تیم" };

        run.SetPlan(
        [
            new BitAiAgentPlanStep("dates", "توافق بر سر تاریخ"),
            new BitAiAgentPlanStep("venue", "رزرو محل"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);

        run.Steps[0].Start();
        run.Steps[0].Block();
        run.RequestInput("کدام هفته برای تیم مناسب است؟", ["هفته اول خرداد", "هفته آخر خرداد"]);

        return run;
    }

    private void RespondToRtlRun(string answer)
    {
        rtlRun.ResolveInput();
        rtlRun.Steps[0].Complete(summary: answer);
        rtlRun.Steps[1].Start();
    }

    private void ResetRtlRun() => rtlRun = CreateRtlRun();



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (session is not null) await session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
