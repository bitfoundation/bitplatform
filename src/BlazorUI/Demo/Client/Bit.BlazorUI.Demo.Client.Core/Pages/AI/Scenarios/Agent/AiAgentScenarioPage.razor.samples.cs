namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Agent;

public partial class AiAgentScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Give it something to research"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoAgentTransports.Research());

private readonly string[] suggestions = ["Do the sources agree on the mechanism?"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """"
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport Research() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    var runId = BitAiId.NewRunId();

    await yielder(new BitAiMessageStartedEvent());
    await yielder(new BitAiAgentRunStartedEvent(runId)
    {
        AgentName = "Research agent",
        Goal = AiScript.LastUserText(request),
    });

    // The whole plan, up front.
    await yielder(new BitAiAgentPlanEvent(runId,
    [
        new BitAiAgentPlanStep("s1", "Search for primary sources"),
        new BitAiAgentPlanStep("s2", "Read the three most relevant"),
        new BitAiAgentPlanStep("s3", "Reconcile the disagreements"),
        new BitAiAgentPlanStep("s4", "Write the summary"),
    ]));

    string[] summaries =
    [
        "11 results, 3 look primary",
        "Two agree on the mechanism, one does not",
        "The disagreement is about scope, not direction",
        "Summary written",
    ];

    // Then each step, started and completed with a summary of what it found.
    for (var i = 0; i < summaries.Length; i++)
    {
        var stepId = $"s{i + 1}";
        await yielder(new BitAiAgentStepStartedEvent(runId, stepId));

        // The search runs on the provider's side: the transport reports the call and its result itself, so the
        // session shows it - and the step names it - without running anything.
        if (i == 0)
        {
            var callId = BitAiId.NewToolCallId();
            await yielder(new BitAiToolCallStartedEvent(callId, "web_search") { Arguments = AiScript.Json("""{"query":"primary sources on the disputed effect"}""") });
            await yielder(new BitAiToolResultEvent(callId, BitAiToolOutcome.FromText("11 results, 3 of them primary studies.")));
        }

        await AiScript.Pause(700 + i * 200, token);
        await yielder(new BitAiAgentStepCompletedEvent(runId, stepId) { Summary = summaries[i] });
    }

    await yielder(new BitAiAgentRunCompletedEvent(runId));

    // And the answer, under the run.
    await foreach (var evt in AiScript.Write("The three primary sources agree on direction and disagree on scope. ...", 4, 11, token))
        await yielder(evt);

    await yielder(new BitAiMessageCompletedEvent());
}));
"""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoAgentTransports.cs", example1TransportCode),
    ];
}
