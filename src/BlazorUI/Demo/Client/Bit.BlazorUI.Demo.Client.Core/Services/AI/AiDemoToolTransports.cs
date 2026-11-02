namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The scripted responses behind the tool scenarios: a model that asks for one tool, one that also asks the user's
/// approval for it, and one that asks for two at once.
/// </summary>
public static class AiDemoToolTransports
{
    /// <summary>
    /// Calls a tool, waits for the session to run it, then answers using the result.
    /// </summary>
    /// <param name="toolName">The tool to call.</param>
    /// <param name="arguments">The arguments to send, as JSON.</param>
    /// <param name="answer">The answer to give once the tool has returned.</param>
    /// <param name="declinedAnswer">
    /// The answer to give when the call did not run - the user declined it, or it failed - in place of a generic one.
    /// </param>
    /// <returns>The transport.</returns>
    /// <remarks>
    /// The transport does not execute the tool. It asks for one, and on the next round trip it can read the result off
    /// the message it is given. That split is what lets an application decide where tools run without the transport
    /// knowing - and the result is read, not just noticed: a call the user declined comes back as a failure, which the
    /// answer explains rather than reporting what the tool would have done.
    /// </remarks>
    public static IBitAiTransport CallsTool(string toolName, string arguments, string answer, string? declinedAnswer = null) =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var outcome = AiScript.ToolOutcomeThisTurn(request);

            await yielder(new BitAiMessageStartedEvent());

            if (outcome is null)
            {
                await yielder(new BitAiStatusEvent("Deciding what to look up"));
                await AiScript.Pause(500, token);

                var callId = BitAiId.NewToolCallId();
                await yielder(new BitAiToolCallStartedEvent(callId, toolName));

                foreach (var slice in AiScript.Slice(arguments, 6))
                {
                    await AiScript.Pause(40, token);
                    await yielder(new BitAiToolCallDeltaEvent(callId, slice));
                }

                await yielder(new BitAiToolCallCompletedEvent(callId));
                await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
                return;
            }

            var reply = outcome.IsSuccess ? answer : declinedAnswer ?? $"I did not run {toolName}, so nothing was changed.";

            await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);
            await yielder(AiScript.Usage(request, reply));
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Calls a tool and asks for the user's decision on it itself, the way a provider whose own policy requires consent
    /// does - whatever the application's approval policy would have decided - then answers using the outcome.
    /// </summary>
    /// <param name="toolName">The tool to call.</param>
    /// <param name="arguments">The arguments to send, as JSON.</param>
    /// <param name="reason">Why the decision is asked for, shown in the approval prompt.</param>
    /// <param name="answer">The answer to give once the tool has returned.</param>
    /// <param name="declinedAnswer">The answer to give when the call did not run.</param>
    /// <returns>The transport.</returns>
    /// <remarks>
    /// The <see cref="BitAiToolApprovalRequestedEvent"/> puts the call in <see cref="BitAiToolCallStatus.AwaitingApproval"/>
    /// with its reason, and the session that runs it waits for the user as it would for its own policy.
    /// </remarks>
    public static IBitAiTransport CallsToolAskingApproval(string toolName, string arguments, string reason, string answer, string declinedAnswer) =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var outcome = AiScript.ToolOutcomeThisTurn(request);

            await yielder(new BitAiMessageStartedEvent());

            if (outcome is null)
            {
                await AiScript.Pause(400, token);

                var callId = BitAiId.NewToolCallId();
                await yielder(new BitAiToolCallStartedEvent(callId, toolName) { Arguments = AiScript.Json(arguments) });
                await yielder(new BitAiToolApprovalRequestedEvent(callId) { Reason = reason });
                await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
                return;
            }

            var reply = outcome.IsSuccess ? answer : declinedAnswer;

            await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);
            await yielder(AiScript.Usage(request, reply));
            await yielder(new BitAiMessageCompletedEvent());
        }));

    /// <summary>
    /// Calls two tools at once, which is where a serial tool UI falls apart.
    /// </summary>
    /// <returns>The transport.</returns>
    public static IBitAiTransport CallsTwoTools() =>
        new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
        {
            var alreadyCalled = AiScript.HasToolResultThisTurn(request);

            await yielder(new BitAiMessageStartedEvent());

            if (alreadyCalled is false)
            {
                await AiScript.Pause(300, token);

                var first = BitAiId.NewToolCallId();
                var second = BitAiId.NewToolCallId();

                await yielder(new BitAiToolCallStartedEvent(first, "get_weather")
                {
                    Arguments = AiScript.Json("""{"city":"Oslo"}"""),
                });
                await AiScript.Pause(120, token);
                await yielder(new BitAiToolCallStartedEvent(second, "get_weather")
                {
                    Arguments = AiScript.Json("""{"city":"Lisbon"}"""),
                });

                await yielder(new BitAiMessageCompletedEvent(BitAiFinishReason.ToolCalls));
                return;
            }

            const string answer =
                "Oslo is cold and grey; Lisbon is mild and clear. If the trip is about the weather, " +
                "the decision makes itself.";

            await foreach (var evt in AiScript.Write(answer, 4, 12, token)) await yielder(evt);
            await yielder(new BitAiMessageCompletedEvent());
        }));
}
