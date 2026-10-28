namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AgentStepView;

public partial class BitAiAgentStepViewDemo
{
    private readonly string example1RazorCode = """
<BitButton OnClick="RunBasicStep" Disabled="isBasicRunning">Run the step</BitButton>

<BitAiAgentStepView Step="basicStep" />
""";
    private readonly string example1CsharpCode = """
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
""";

    private readonly string example2RazorCode = """
<div>
    @foreach (var step in statusSteps)
    {
        <BitAiAgentStepView Step="step" />
    }
</div>
""";
    private readonly string example2CsharpCode = """
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
""";

    private readonly string example3RazorCode = """
<BitAiAgentStepView Step="detailStep" />
""";
    private readonly string example3CsharpCode = """
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
""";

    private readonly string example4RazorCode = """
<BitButton OnClick="RunToolStep" Disabled="isToolRunning">Run the step</BitButton>

<div>
    <BitAiAgentStepView Step="toolStep" Conversation="toolConversation" />
</div>

<div>
    <BitAiAgentStepView Step="toolStep" />
</div>
""";
    private readonly string example4CsharpCode = """
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
""";

    private readonly string example5RazorCode = """
<BitAiAgentStepView Disabled Step="disabled.Step" Conversation="disabled.Conversation" />
""";
    private readonly string example5CsharpCode = """
private readonly (BitAiConversation Conversation, BitAiAgentStep Step) disabled = CreateDisabledStep();

private static (BitAiConversation Conversation, BitAiAgentStep Step) CreateDisabledStep()
{
    var conversation = new BitAiConversation();
    var message = conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
    var call = message.AddPart(new BitAiToolCallPart(BitAiId.NewToolCallId(), "run_tests"));
    call.SetResult(BitAiToolOutcome.Failure("3 of 812 tests failed."));

    var step = new BitAiAgentStep("test", "Run the test suite");
    step.Start();
    step.AddToolCall(call.ToolCallId);
    step.Complete(BitAiAgentStepStatus.Failed, error: new BitAiError("3 of 812 tests failed.")
    {
        Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund",
    });

    return (conversation, step);
}
""";

    private readonly string example6RazorCode = """
<BitParams Parameters="stepViewParams">
    <BitAiAgentStepView Step="cascadingSteps[0]" />
    <BitAiAgentStepView Step="cascadingSteps[1]" />
    <BitAiAgentStepView Step="cascadingSteps[2]" Styles="@(new() { Summary = "font-weight: bold" })" />
</BitParams>
""";
    private readonly string example6CsharpCode = """
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
""";

    private readonly string example7RazorCode = """
<style>
    .custom-class {
        padding-inline: 1rem;
        border-inline-start: 0.25rem solid #0ea5e9;
    }

    .custom-title {
        font-weight: bold;
        text-transform: uppercase;
    }

    .custom-summary {
        color: #0369a1;
        font-style: italic;
    }
</style>

<BitAiAgentStepView Step="styleSteps[0]" Style="padding-inline-start: 1rem; background-color: #0ea5e911" />
<BitAiAgentStepView Step="styleSteps[1]"
                    Class="custom-class"
                    Classes="@(new() { Title = "custom-title", Summary = "custom-summary" })"
                    Styles="@(new() { Duration = "color: color-mix(in srgb, #0ea5e9 60%, var(--bit-clr-fg-pri))" })" />

<div style="--bit-AiAgentStepView-marker-size: 1.25rem;
            --bit-AiAgentStepView-completed-color: #7c3aed;
            --bit-AiAgentStepView-running-color: #db2777;
            --bit-AiAgentStepView-line-color: #c4b5fd;
            --bit-AiAgentStepView-gap: 1rem;
            --bit-AiAgentStepView-title-font-size: 1rem">
    @foreach (var step in styleSteps)
    {
        <BitAiAgentStepView Step="step" />
    }
</div>
""";
    private readonly string example7CsharpCode = """
private readonly BitAiAgentStep[] styleSteps = CreateStyleSteps();

private static BitAiAgentStep[] CreateStyleSteps()
{
    var venue = new BitAiAgentStep("venue", "Book the venue");
    venue.Complete(summary: "The lakeside hall, for 40");

    var invitations = new BitAiAgentStep("invitations", "Send the invitations");
    invitations.Start();

    return [venue, invitations];
}
""";

    private readonly string example8RazorCode = """
<div dir="rtl">
    @foreach (var step in rtlSteps)
    {
        <BitAiAgentStepView Step="step" />
    }
</div>

<BitAiAgentStepView Dir="BitDir.Rtl" Step="rtlSteps[0]" />
""";
    private readonly string example8CsharpCode = """
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
""";
}
