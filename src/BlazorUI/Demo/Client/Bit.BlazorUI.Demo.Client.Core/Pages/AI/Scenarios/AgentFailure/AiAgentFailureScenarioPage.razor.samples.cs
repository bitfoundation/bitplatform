namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.AgentFailure;

public partial class AiAgentFailureScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ship the release"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoAgentTransports.Fails());

private readonly string[] suggestions = ["Deploy the current branch"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """
public static IBitAiTransport Fails() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    const string runId = "run-deploy";

    await yielder(new BitAiMessageStartedEvent());
    await yielder(new BitAiAgentRunStartedEvent(runId) { AgentName = "Deploy agent", Goal = "Ship the release" });
    await yielder(new BitAiAgentPlanEvent(runId,
    [
        new BitAiAgentPlanStep("s1", "Build"),
        new BitAiAgentPlanStep("s2", "Run the test suite"),
        new BitAiAgentPlanStep("s3", "Deploy"),
    ]));

    await yielder(new BitAiAgentStepStartedEvent(runId, "s1"));
    await yielder(new BitAiAgentStepCompletedEvent(runId, "s1") { Summary = "Build succeeded in 41s" });

    // The failure belongs to the step, not to the whole run.
    await yielder(new BitAiAgentStepStartedEvent(runId, "s2"));
    await yielder(new BitAiAgentStepCompletedEvent(runId, "s2", BitAiAgentStepStatus.Failed)
    {
        Error = new BitAiError("3 of 812 tests failed.")
        {
            Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund",
            IsRetryable = true,
        },
    });

    await yielder(new BitAiAgentRunCompletedEvent(runId, BitAiAgentRunStatus.Failed));

    await foreach (var evt in AiScript.Write("I stopped before deploying. Three checkout tests fail, and all three touch money.", 4, 12, token))
        await yielder(evt);

    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoAgentTransports.cs", example1TransportCode),
    ];
}
