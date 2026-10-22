namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.AgentInput;

public partial class AiAgentInputScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Start the migration"
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoAgentTransports.AsksForInput());

private readonly string[] suggestions = ["Migrate the database to the new schema"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """
public static IBitAiTransport AsksForInput() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    // One id for the run across both turns, so the second turn resumes it.
    const string runId = "run-migration";
    var answered = request.Messages.Count(m => m.Role.IsUser) > 1;

    await yielder(new BitAiMessageStartedEvent());

    if (answered is false)
    {
        await yielder(new BitAiAgentRunStartedEvent(runId) { AgentName = "Migration agent", Goal = "Move the database to the new schema" });
        await yielder(new BitAiAgentPlanEvent(runId,
        [
            new BitAiAgentPlanStep("s1", "Inspect the current schema"),
            new BitAiAgentPlanStep("s2", "Choose a downtime window"),
            new BitAiAgentPlanStep("s3", "Run the migration"),
        ]));

        await yielder(new BitAiAgentStepStartedEvent(runId, "s1"));
        await yielder(new BitAiAgentStepCompletedEvent(runId, "s1") { Summary = "14 tables, 2 need rewriting" });

        // Stop at step two, with a question and the answers it can take in one tap.
        await yielder(new BitAiAgentStepStartedEvent(runId, "s2"));
        await yielder(new BitAiAgentInputRequestedEvent(runId,
            "Two tables need a rewrite, which needs downtime. When should I take it?")
        {
            Suggestions = ["Tonight after 22:00", "Saturday morning", "Ask me again later"],
        });

        await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.Stop));
        return;
    }

    // The answer arrived as a user message: carry on from step two.
    await yielder(new BitAiAgentStepCompletedEvent(runId, "s2") { Summary = "Window agreed" });
    await yielder(new BitAiAgentStepStartedEvent(runId, "s3"));
    await yielder(new BitAiAgentStepCompletedEvent(runId, "s3") { Summary = "14 tables migrated" });
    await yielder(new BitAiAgentRunCompletedEvent(runId));

    await foreach (var evt in AiScript.Write("Booked. I will run the migration in that window and report back.", 4, 12, token))
        await yielder(evt);

    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoAgentTransports.cs", example1TransportCode),
    ];
}
