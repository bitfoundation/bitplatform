namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ToolApproval;

public partial class BitAiToolApprovalDemo
{
    private readonly string example1RazorCode = """
@if (basicCall.Status is BitAiToolCallStatus.AwaitingApproval)
{
    <BitAiToolApproval Call="basicCall" OnDecision="d => basicDecision = d" />
}
else
{
    <div>
        Decision: <b>@basicDecision</b>, status of the call: <b>@basicCall.Status</b>
        <br /><br />
        <BitButton Variant="BitVariant.Outline" OnClick="ResetBasic">Ask again</BitButton>
    </div>
}
""";
    private readonly string example1CsharpCode = """
private BitAiToolCallPart basicCall = AwaitingApproval("archive_thread", "Archive the thread", BitAiToolRisk.Write);
private BitAiApprovalDecision basicDecision;

private void ResetBasic()
{
    basicCall = AwaitingApproval("archive_thread", "Archive the thread", BitAiToolRisk.Write);
    basicDecision = BitAiApprovalDecision.Undecided;
}

// A call the way a session leaves it while it waits: its arguments complete, its definition resolved.
private static BitAiToolCallPart AwaitingApproval(string toolName, string title, BitAiToolRisk risk, string? reason = null)
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName)
    {
        Definition = new BitAiToolDefinition(toolName) { Title = title, Risk = risk, RequiresApproval = true },
        ApprovalReason = reason,
    };

    call.CompleteArguments();
    call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

    return call;
}
""";

    private readonly string example2RazorCode = """
<BitAiToolApproval Call="reasonCall" />

<BitAiToolApproval Call="reasonCall" AriaLabel="Confirm sending the weekly summary" />
""";
    private readonly string example2CsharpCode = """
private readonly BitAiToolCallPart reasonCall = AwaitingApproval("send_email", "Send email", BitAiToolRisk.Write,
    "Send the weekly summary to the 3 people on the thread?");

private static BitAiToolCallPart AwaitingApproval(string toolName, string title, BitAiToolRisk risk, string? reason = null)
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName)
    {
        Definition = new BitAiToolDefinition(toolName) { Title = title, Risk = risk, RequiresApproval = true },
        ApprovalReason = reason,
    };

    call.CompleteArguments();
    call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

    return call;
}
""";

    private readonly string example3RazorCode = """
<BitAiToolApproval Call="readOnlyCall" OnDecision="@(d => riskDecision = $"{d} (Read the logs)")" />
<BitAiToolApproval Call="writeCall" OnDecision="@(d => riskDecision = $"{d} (Send email)")" />
<BitAiToolApproval Call="destructiveCall" OnDecision="@(d => riskDecision = $"{d} (Delete records)")" />

<div>Last decision: <b>@riskDecision</b></div>
""";
    private readonly string example3CsharpCode = """
private readonly BitAiToolCallPart readOnlyCall = AwaitingApproval("read_logs", "Read the logs", BitAiToolRisk.ReadOnly);
private readonly BitAiToolCallPart writeCall = AwaitingApproval("send_email", "Send email", BitAiToolRisk.Write);
private readonly BitAiToolCallPart destructiveCall = AwaitingApproval("delete_records", "Delete records", BitAiToolRisk.Destructive,
    "Permanently delete the 128 archived records? This cannot be undone.");
private string riskDecision = "none yet";

private static BitAiToolCallPart AwaitingApproval(string toolName, string title, BitAiToolRisk risk, string? reason = null)
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName)
    {
        Definition = new BitAiToolDefinition(toolName) { Title = title, Risk = risk, RequiresApproval = true },
        ApprovalReason = reason,
    };

    call.CompleteArguments();
    call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

    return call;
}
""";

    private readonly string example4RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="AskToDeleteAsync" Disabled="session.IsGenerating">Delete the archived records</BitButton>

@if (SessionCall is { } call)
{
    @if (call.Status is BitAiToolCallStatus.AwaitingApproval)
    {
        <BitAiToolApproval Call="call" Session="session" />
    }

    <div>
        Status of the call: <b>@call.Status</b>
        @if (call.Result?.GetDisplayText() is { } outcome)
        {
            <span> - @outcome</span>
        }
    </div>
}
""";
    private readonly string example4CsharpCode = """"
[Inject] private IBitAiToolRegistry ToolRegistry { get; set; } = default!;

private BitAiRenderScheduler sessionRenders = default!;
private BitAiChatSession session = default!;

// The call of the latest turn, whatever it is waiting for.
private BitAiToolCallPart? SessionCall => session.Conversation.Messages
    .LastOrDefault(m => m.Role.IsAssistant)?
    .PartsOf<BitAiToolCallPart>()
    .FirstOrDefault();

protected override void OnInitialized()
{
    // At most one render of the page per frame interval, however often the session changes.
    sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

    // A scripted model that asks for delete_records - registered with AddBitBlazorUIAIServices and declared
    // Destructive - so the session stops and waits before running it. A policy of its own keeps an "Always allow"
    // to this session.
    session = new BitAiChatSession(
        AiDemoToolTransports.CallsTool(
            "delete_records",
            """{"filter":"status = 'archived'"}""",
            "Done."),
        tools: ToolRegistry,
        approvalPolicy: new BitAiDefaultToolApprovalPolicy());
    session.StateChanged += sessionRenders.Request;
}

private Task AskToDeleteAsync() => session.SubmitAsync("Delete the archived records");

public async ValueTask DisposeAsync()
{
    await session.DisposeAsync();
    await sessionRenders.DisposeAsync();
}
"""";

    private readonly string example5RazorCode = """
<BitAiToolApproval Disabled Call="destructiveCall" />
""";

    private readonly string example6RazorCode = """
<BitParams Parameters="approvalParams">
    <BitAiToolApproval Call="writeCall" />
    <BitAiToolApproval Call="destructiveCall" />
    <BitAiToolApproval Call="readOnlyCall" Styles="@(new() { Text = "font-weight: bold" })" />
</BitParams>
""";
    private readonly string example6CsharpCode = """
private readonly BitAiToolApprovalParams[] approvalParams =
[
    new() { Styles = new() { ApproveButton = new() { Root = "border-radius: 1rem" }, Text = "font-style: italic" } },
];
""";

    private readonly string example7RazorCode = """
<style>
    .custom-approval {
        border: 2px dashed #b45309;
        background-color: #fffbeb;
    }

    .custom-approval-root {
        border-inline-start: 4px solid #be123c;
    }

    .custom-approval-actions {
        width: 100%;
        justify-content: flex-end;
    }
</style>

<BitAiToolApproval Call="writeCall" Style="border: 1px solid #b45309" />

<BitAiToolApproval Call="writeCall" Class="custom-approval" />

<BitAiToolApproval Call="writeCall"
                   Styles="@(new() { Text = "font-weight: bold",
                                     ApproveButton = new() { Root = "border-radius: 1rem" } })" />

<BitAiToolApproval Call="destructiveCall"
                   Classes="@(new() { Root = "custom-approval-root",
                                      Actions = "custom-approval-actions" })" />

<div style="--bit-AiToolApproval-background: #ecfeff; --bit-AiToolApproval-destructive-background: #fff1f2; --bit-AiToolApproval-radius: 0; --bit-AiToolApproval-padding: 1rem 1.5rem; --bit-AiToolApproval-gap: 1rem;">
    <BitAiToolApproval Call="writeCall" />
    <BitAiToolApproval Call="destructiveCall" />
</div>
""";

    private readonly string example8RazorCode = """
<BitAiOptionsProvider UiOptions="persianOptions">
    <BitAiToolApproval Dir="BitDir.Rtl" Call="rtlCall" OnDecision="d => rtlDecision = d" />
</BitAiOptionsProvider>

<div>Decision: <b>@rtlDecision</b></div>
""";
    private readonly string example8CsharpCode = """
private readonly BitAiUiOptions persianOptions = BitAiUiOptions.Default with { Strings = new PersianAiStrings() };
private readonly BitAiToolCallPart rtlCall = AwaitingApproval("delete_records", "حذف رکوردها", BitAiToolRisk.Destructive,
    "۱۲۸ رکورد بایگانی‌شده برای همیشه حذف شوند؟ این کار برگشت‌پذیر نیست.");
private BitAiApprovalDecision rtlDecision;

private static BitAiToolCallPart AwaitingApproval(string toolName, string title, BitAiToolRisk risk, string? reason = null)
{
    var call = new BitAiToolCallPart(BitAiId.NewToolCallId(), toolName)
    {
        Definition = new BitAiToolDefinition(toolName) { Title = title, Risk = risk, RequiresApproval = true },
        ApprovalReason = reason,
    };

    call.CompleteArguments();
    call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

    return call;
}

// The words of the prompt, in Persian.
private sealed class PersianAiStrings : BitAiStrings
{
    public override string ApproveTool(string tool) => $"اجازه‌ی اجرای {tool} داده شود؟";
    public override string Approve => "اجازه";
    public override string ApproveAlways => "همیشه اجازه";
    public override string Reject => "رد";
}
""";
}
