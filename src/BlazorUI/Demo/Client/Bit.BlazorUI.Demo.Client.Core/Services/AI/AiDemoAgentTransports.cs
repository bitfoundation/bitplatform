namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The scripted agent runs behind the agent scenarios: one that plans and works through its plan, one that stops to ask
/// the user something, and one whose step fails.
/// </summary>
public static class AiDemoAgentTransports
{
    /// <summary>
    /// A multi-step agent: it plans, works through the plan, and reports as it goes.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Research() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var runId = BitAiId.NewRunId();

            await yielder(new BitAiMessageStartedEvent());
            await yielder(new BitAiAgentRunStartedEvent(runId)
            {
                AgentName = "Research agent",
                Goal = AiScript.LastUserText(request),
            });

            await AiScript.Pause(400, token);
            await yielder(new BitAiAgentPlanEvent(runId,
            [
                new BitAiAgentPlanStep("s1", "Search for primary sources"),
                new BitAiAgentPlanStep("s2", "Read the three most relevant"),
                new BitAiAgentPlanStep("s3", "Reconcile the disagreements"),
                new BitAiAgentPlanStep("s4", "Write the summary"),
            ]));

            var summaries = new[]
            {
                "11 results, 3 look primary",
                "Two agree on the mechanism, one does not",
                "The disagreement is about scope, not direction",
                "Summary written",
            };

            for (var i = 0; i < summaries.Length; i++)
            {
                var stepId = $"s{i + 1}";
                await AiScript.Pause(500, token);
                await yielder(new BitAiAgentStepStartedEvent(runId, stepId));
                await AiScript.Pause(700 + i * 200, token);
                await yielder(new BitAiAgentStepCompletedEvent(runId, stepId) { Summary = summaries[i] });
            }

            await yielder(new BitAiAgentRunCompletedEvent(runId));

            const string answer = """
                The three primary sources agree on direction and disagree on scope.

                Two of them measure the effect inside a single population and find it clearly. The
                third measures across populations and finds it weakly, which is what you would expect
                if the effect is real but the between-group variance is larger than the within-group
                variance.

                That is not a contradiction. It is a reason to be careful about which claim you make.
                """;

            await foreach (var evt in AiScript.Write(answer, 4, 11, token)) await yielder(evt);
            await yielder(AiScript.Usage(request, answer));
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// An agent that stops mid-run to ask the user something.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport AsksForInput() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            const string runId = "run-migration";
            var answered = request.Messages.Count(m => m.Role.IsUser) > 1;

            await yielder(new BitAiMessageStartedEvent());

            if (answered is false)
            {
                await yielder(new BitAiAgentRunStartedEvent(runId)
                {
                    AgentName = "Migration agent",
                    Goal = "Move the database to the new schema",
                });

                await yielder(new BitAiAgentPlanEvent(runId,
                [
                    new BitAiAgentPlanStep("s1", "Inspect the current schema"),
                    new BitAiAgentPlanStep("s2", "Choose a downtime window"),
                    new BitAiAgentPlanStep("s3", "Run the migration"),
                ]));

                await AiScript.Pause(400, token);
                await yielder(new BitAiAgentStepStartedEvent(runId, "s1"));
                await AiScript.Pause(900, token);
                await yielder(new BitAiAgentStepCompletedEvent(runId, "s1") { Summary = "14 tables, 2 need rewriting" });

                await AiScript.Pause(300, token);
                await yielder(new BitAiAgentStepStartedEvent(runId, "s2"));
                await yielder(new BitAiAgentInputRequestedEvent(runId,
                    "Two tables need a rewrite, which needs downtime. When should I take it?")
                {
                    Suggestions = ["Tonight after 22:00", "Saturday morning", "Ask me again later"],
                });

                await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.Stop));
                return;
            }

            await yielder(new BitAiAgentStepCompletedEvent(runId, "s2") { Summary = "Window agreed" });
            await AiScript.Pause(400, token);
            await yielder(new BitAiAgentStepStartedEvent(runId, "s3"));
            await AiScript.Pause(900, token);
            await yielder(new BitAiAgentStepCompletedEvent(runId, "s3") { Summary = "14 tables migrated" });
            await yielder(new BitAiAgentRunCompletedEvent(runId));

            const string answer = "Booked. I will run the migration in that window and report back.";
            await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// An agent whose second step fails, so the failure has to be legible.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport Fails() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
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

            await AiScript.Pause(400, token);
            await yielder(new BitAiAgentStepStartedEvent(runId, "s1"));
            await AiScript.Pause(800, token);
            await yielder(new BitAiAgentStepCompletedEvent(runId, "s1") { Summary = "Build succeeded in 41s" });

            await AiScript.Pause(300, token);
            await yielder(new BitAiAgentStepStartedEvent(runId, "s2"));
            await AiScript.Pause(1100, token);
            await yielder(new BitAiAgentStepCompletedEvent(runId, "s2", BitAiAgentStepStatus.Failed)
            {
                Error = new BitAiError("3 of 812 tests failed.")
                {
                    Detail = "Checkout.AppliesDiscount\nCheckout.RoundsTax\nCheckout.HandlesRefund",
                    IsRetryable = true,
                },
            });

            await yielder(new BitAiAgentRunCompletedEvent(runId, BitAiAgentRunStatus.Failed));

            const string answer = """
                I stopped before deploying. Three checkout tests fail, and all three touch money.

                The failures look related: they share `Checkout.PriceCalculator`, which changed in the
                last commit on this branch.
                """;

            await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiMessageCompletedEvent());
        }));
}
