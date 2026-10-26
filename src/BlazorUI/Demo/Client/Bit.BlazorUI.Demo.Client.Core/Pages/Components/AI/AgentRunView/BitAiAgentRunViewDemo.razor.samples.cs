namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AgentRunView;

public partial class BitAiAgentRunViewDemo
{
    private readonly string example1RazorCode = """
<BitButton OnClick="StartBasicRun" Disabled="isBasicRunning">Start the run</BitButton>

@if (basicRun is not null)
{
    <BitAiAgentRunView Run="basicRun" />
}
""";
    private readonly string example1CsharpCode = """
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
""";

    private readonly string example2RazorCode = """
<BitButton OnClick="StartFailingRun" Disabled="isFailingRunning">Deploy the release</BitButton>

@if (failingRun is not null)
{
    <BitAiAgentRunView Run="failingRun" />
}
""";
    private readonly string example2CsharpCode = """
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

    // Ending the run as failed completes the steps it will no longer reach (Deploy) as Skipped.
    run.SetError(new BitAiError("The run stopped before deploying.")
    {
        Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund",
    });

    isFailingRunning = false;
}
""";

    private readonly string example3RazorCode = """
<BitButton OnClick="StartAskingRun" Disabled="isAskingRunning">Start the migration</BitButton>

@if (askingRun is not null)
{
    <BitAiAgentRunView Run="askingRun" OnRespond="RespondToAskingRun" />
}
""";
    private readonly string example3CsharpCode = """
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
""";

    private readonly string example4RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="() => StartSessionRun(AiDemoAgentTransports.Research())">Research</BitButton>
<BitButton OnClick="() => StartSessionRun(AiDemoAgentTransports.AsksForInput())">Ask the user</BitButton>
<BitButton OnClick="() => StartSessionRun(AiDemoAgentTransports.Fails())">Fail</BitButton>

@if (sessionRun is not null)
{
    <BitAiAgentRunView Run="sessionRun" Session="session" />
}
""";
    private readonly string example4CsharpCode = """
// AiDemoAgentTransports are the scripted agents of this site (an IBitAiTransport each):
// https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Services/AI/AiDemoAgentTransports.cs
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

public async ValueTask DisposeAsync()
{
    if (session is not null) await session.DisposeAsync();
}
""";

    private readonly string example5RazorCode = """
<BitButton OnClick="StartUsageRun" Disabled="isUsageRunning">Start the run</BitButton>

@if (usageRun is not null)
{
    <BitAiOptionsProvider UiOptions="usageOptions">
        <BitAiAgentRunView Run="usageRun" ClockInterval="TimeSpan.FromMilliseconds(100)" />
    </BitAiOptionsProvider>
}
""";
    private readonly string example5CsharpCode = """
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
""";

    private readonly string example6RazorCode = """
<BitButton OnClick="StartCascadingRun" Disabled="isCascadingRunning">Start the run</BitButton>

@if (cascadingRun is not null)
{
    <BitParams Parameters="runViewParams">
        <BitAiAgentRunView Run="cascadingRun" />

        <BitAiAgentRunView Run="cascadingRun" Styles="@(new() { Goal = "font-weight: bold" })" />
    </BitParams>
}
""";
    private readonly string example6CsharpCode = """
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
""";

    private readonly string example7RazorCode = """
<style>
    .custom-class {
        border-width: 2px;
        border-color: #0ea5e9;
    }

    .custom-header {
        color: #0369a1;
        background-color: #e0f2fe;
    }

    .custom-goal {
        font-size: 1rem;
        color: var(--bit-clr-pri-fg);
    }

    .custom-question {
        font-weight: bold;
    }
</style>

<BitButton Variant="BitVariant.Outline" OnClick="ResetStyledRun">Ask again</BitButton>

<BitAiAgentRunView Run="styledRun"
                   OnRespond="RespondToStyledRun"
                   Style="box-shadow: 0 0 1rem #0ea5e966" />

<BitAiAgentRunView Run="styledRun"
                   OnRespond="RespondToStyledRun"
                   Class="custom-class"
                   Classes="@(new() { Header = "custom-header", Goal = "custom-goal", Question = "custom-question" })"
                   Styles="@(new() { Title = "letter-spacing: 0.05em; text-transform: uppercase",
                                     Step = new() { Title = "font-style: italic" } })" />

<BitAiAgentRunView Run="styledRun"
                   OnRespond="RespondToStyledRun"
                   Style="--bit-AiAgentRunView-background: #1e293b;
                          --bit-AiAgentRunView-body-background: #0f172a;
                          --bit-AiAgentRunView-border-color: #334155;
                          --bit-AiAgentRunView-header-color: #e2e8f0;
                          --bit-AiAgentRunView-text-color: #94a3b8;
                          --bit-AiAgentRunView-prompt-background: #3b2f0b;
                          --bit-AiAgentRunView-radius: 1rem;
                          --bit-AiAgentStepView-text-color: #94a3b8;
                          --bit-Button-outline-color: #f1f5f9;
                          --bit-Button-outline-border-color: #64748b;
                          color: #f1f5f9" />
""";
    private readonly string example7CsharpCode = """
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

private async Task RespondToStyledRun(string answer)
{
    // The run answered, which Ask again may replace while it goes on.
    var run = styledRun;

    run.ResolveInput();
    run.Steps[0].Complete(summary: answer);

    var venue = run.Steps[1];
    venue.Start();
    await Task.Delay(1200);
    venue.Complete(summary: "The lakeside hall, for 40");

    run.SetStatus(BitAiAgentRunStatus.Completed);
}

private void ResetStyledRun() => styledRun = CreateStyledRun();
""";

    private readonly string example8RazorCode = """
<BitButton Variant="BitVariant.Outline" OnClick="ResetRtlRun">دوباره بپرس</BitButton>

<BitAiOptionsProvider UiOptions="persianOptions">
    <BitAiAgentRunView Dir="BitDir.Rtl" Run="rtlRun" OnRespond="RespondToRtlRun" />
</BitAiOptionsProvider>
""";
    private readonly string example8CsharpCode = """
private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
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

private async Task RespondToRtlRun(string answer)
{
    var run = rtlRun;

    run.ResolveInput();
    run.Steps[0].Complete(summary: answer);

    var venue = run.Steps[1];
    venue.Start();
    await Task.Delay(1200);
    venue.Complete(summary: "سالن کنار دریاچه، برای ۴۰ نفر");

    run.SetStatus(BitAiAgentRunStatus.Completed);
}

private void ResetRtlRun() => rtlRun = CreateRtlRun();

// The words of the view, in Persian.
private sealed class PersianAiStrings : BitAiStrings
{
    public override string Plan => "برنامه";
    public override string AgentRunning => "در حال اجرا";
    public override string WaitingForYou => "منتظر پاسخ شما";
    public override string AgentCompleted => "انجام شد";
    public override string AnswerPlaceholder => "پاسخ خود را بنویسید...";
    public override string Answer => "پاسخ";
    public override string Elapsed => "زمان سپری‌شده";
    public override string ToolCalls => "فراخوانی ابزارها";
    public override string StepsCompleted(int done, int total) => $"{done} از {total}";
    public override string AgentStepStatus(BitAiAgentStepStatus status) => status switch
    {
        BitAiAgentStepStatus.Running => "در حال انجام",
        BitAiAgentStepStatus.Blocked => "منتظر شما",
        BitAiAgentStepStatus.Completed => "انجام شد",
        BitAiAgentStepStatus.Failed => "ناموفق",
        BitAiAgentStepStatus.Skipped => "کنار گذاشته شد",
        _ => "شروع نشده",
    };
    public override string FormatDuration(TimeSpan duration) => duration.TotalSeconds < 1
        ? $"{duration.TotalMilliseconds:F0} میلی‌ثانیه"
        : $"{duration.TotalSeconds:0} ثانیه";
}
""";
}
