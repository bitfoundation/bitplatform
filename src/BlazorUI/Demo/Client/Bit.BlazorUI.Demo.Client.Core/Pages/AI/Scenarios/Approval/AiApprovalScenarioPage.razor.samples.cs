namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Approval;

public partial class AiApprovalScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask for something destructive"
               EmptyBody="delete_records is declared destructive, so the session will not run it unattended."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """"
private BitAiChatSession? session;

private readonly string[] suggestions = ["Delete the archived records"];

[Inject] private IBitAiToolRegistry Tools { get; set; } = default!;
[Inject] private IBitAiToolApprovalPolicy Policy { get; set; } = default!;

protected override void OnInitialized() => session = new BitAiChatSession(
    AiDemoToolTransports.CallsTool(
        "delete_records",
        """{"filter":"status = 'archived' AND updated < '2024-01-01'"}""",
        "Deleted 128 archived records. Nothing else was touched."),
    tools: Tools,
    approvalPolicy: Policy);

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
"""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // Destructive: the default approval policy stops every call of it for a human decision.
    ai.AddTool(BitAiTool.Create(
        "delete_records",
        "Permanently delete records matching a filter.",
        AiDemoJsonContext.Default.DeleteArgs,
        AiDemoJsonContext.Default.DeleteResult,
        async (args, token) =>
        {
            await Task.Delay(600, token);
            return new DeleteResult(args.Filter, 128);
        },
        definition => definition with
        {
            Title = "Delete records",
            Icon = BitIconName.Delete,
            Risk = BitAiToolRisk.Destructive,
        }));
});
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
    ];
}
