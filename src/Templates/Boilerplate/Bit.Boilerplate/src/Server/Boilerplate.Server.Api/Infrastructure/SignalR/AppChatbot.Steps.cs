using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

public partial class AppChatbot
{
    /// <summary>
    /// An answer's steps: the calls the panel shows as a line saying what the assistant did (See
    /// <see cref="AiChatProtocol.Tools.IsStep"/>). An answer with two or more of them shows them as one timeline instead, a
    /// run of its message, while a single one stays a line. Which it is shows only once a second one starts, so that's when
    /// the run starts, with the first step in it. Each step's id is its call's, which is how the panel knows which lines the
    /// timeline stands in for (See AppAiChat.IsStep).
    /// </summary>
    private sealed class AnswerSteps(IStringLocalizer<AppStrings> localizer)
    {
        private readonly string runId = BitAiId.New("run");
        private readonly List<BitAiToolCallStartedEvent> calls = [];
        private readonly Dictionary<string, BitAiToolResultEvent> results = [];
        private bool started;

        /// <summary>What goes ahead of <paramref name="evt"/>: the run and its steps, so a call opens on its step.</summary>
        public IEnumerable<BitAiEvent> Before(BitAiEvent evt)
        {
            switch (evt)
            {
                case BitAiToolCallStartedEvent call when AiChatProtocol.Tools.IsStep(call.ToolName):
                    calls.Add(call);

                    if (started)
                    {
                        yield return new BitAiAgentStepStartedEvent(runId, call.ToolCallId, Title(call));
                    }
                    else if (calls.Count is 2)
                    {
                        started = true;

                        yield return new BitAiAgentRunStartedEvent(runId);

                        var first = calls[0];
                        yield return new BitAiAgentStepStartedEvent(runId, first.ToolCallId, Title(first));

                        if (results.TryGetValue(first.ToolCallId, out var result))
                        {
                            yield return Completed(result);

                            // Again, so the first call's line is drawn once more and the panel finds it a step now (See
                            // AppAiChat.IsStep): a call's line is drawn again only when the call changes.
                            yield return result;
                        }

                        yield return new BitAiAgentStepStartedEvent(runId, call.ToolCallId, Title(call));
                    }
                    break;

                case BitAiMessageCompletedEvent when started:
                    yield return new BitAiAgentRunCompletedEvent(runId);
                    break;
            }
        }

        /// <summary>What follows <paramref name="evt"/>: the end of the step whose call it answered.</summary>
        public IEnumerable<BitAiEvent> After(BitAiEvent evt)
        {
            if (evt is not BitAiToolResultEvent result || calls.Any(call => call.ToolCallId == result.ToolCallId) is false) yield break;

            results[result.ToolCallId] = result;

            if (started)
            {
                yield return Completed(result);
            }
        }

        private BitAiAgentStepCompletedEvent Completed(BitAiToolResultEvent result)
            => new(runId, result.ToolCallId, result.Outcome.IsSuccess ? BitAiAgentStepStatus.Completed : BitAiAgentStepStatus.Failed);

        /// <summary>The words the panel shows the call's line with (See AppAiChatTools.Describe), or the tool's own name.</summary>
        private string Title(BitAiToolCallStartedEvent call)
            => AiChatProtocol.Tools.TitleKey(call.ToolName) is { } key ? localizer[key] : call.ToolName;
    }
}
