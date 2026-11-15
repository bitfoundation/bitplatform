//+:cnd:noEmit
using System.Threading.Channels;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Infrastructure.SignalR;

public partial class AppChatbot
{
    /// <summary>
    /// Where a tool shows what it shows in the conversation - products, a contact form, an approval, suggestions to tap.
    /// A tool says what to show and the panel draws it from the tool call (See the tool renderers of AppAiChat), so the
    /// tools never name a component: in a text answer it streams with the answer (<see cref="TurnUi"/>), and in a voice
    /// call, which streams no answer, it goes to the panel over the user's SignalR connection (<see cref="VoiceCallUi"/>).
    /// </summary>
    private abstract class ChatbotUi
    {
        public static readonly ChatbotUi None = new NoUi();

        /// <summary>
        /// Shows what a call of <paramref name="toolName"/> stands for: <paramref name="arguments"/> as the call's (the form's
        /// draft), <paramref name="shown"/> as its result (the products), in place of what the model is told. What the
        /// model reads of it on later turns is <paramref name="replay"/>. False when it could not be shown.
        /// </summary>
        public abstract ValueTask<bool> Show(string toolName, JsonElement? arguments, JsonElement? shown, string replay, CancellationToken cancellationToken);

        /// <summary>Offers <paramref name="suggestions"/> under the answer, as prompts to tap.</summary>
        public abstract ValueTask<bool> Suggest(string[] suggestions, CancellationToken cancellationToken);

        /// <summary>
        /// Asks the user to approve a call of <paramref name="toolName"/>, for the reason the panel shows, and waits for
        /// their decision. Fails closed: a cancellation or an error is <see cref="BitAiApprovalDecision.Undecided"/>.
        /// </summary>
        public abstract Task<BitAiApprovalDecision> AskApproval(string toolName, string reason, CancellationToken cancellationToken);

        /// <summary>The call the tool runs in: the model's, or - in a voice call, whose calls the provider names - one of its own.</summary>
        protected static string CurrentCallId() => FunctionInvokingChatClient.CurrentContext?.CallContent.CallId ?? BitAiId.New("call");
    }

    /// <summary>An MCP client's call, from DevMcp, which has no conversation to show anything in.</summary>
    private sealed class NoUi : ChatbotUi
    {
        public override ValueTask<bool> Show(string toolName, JsonElement? arguments, JsonElement? shown, string replay, CancellationToken cancellationToken) => ValueTask.FromResult(false);

        public override ValueTask<bool> Suggest(string[] suggestions, CancellationToken cancellationToken) => ValueTask.FromResult(false);

        public override Task<BitAiApprovalDecision> AskApproval(string toolName, string reason, CancellationToken cancellationToken) => Task.FromResult(BitAiApprovalDecision.Undecided);
    }

    /// <summary>
    /// A text answer: what a tool shows streams with the answer, between its call and its result, and an approval is
    /// waited for on the BitAiApprovalBroker, which the panel's decision reaches over the same connection (BitAiHub's
    /// SendBitAiApproval).
    /// </summary>
    private sealed class TurnUi(ChannelWriter<BitAiEvent> events, BitAiApprovalBroker approvalBroker) : ChatbotUi
    {
        private readonly Lock gate = new();
        private readonly Dictionary<string, JsonElement> shownByCall = [];
        private readonly List<string> shown = [];

        /// <summary>A tool was called since the model last wrote, so what it writes next is a paragraph of its own.</summary>
        public bool ToolRanSinceText { get; set; }

        /// <summary>What the model reads of everything the tools showed, in the order they showed it (See <see cref="AiChatProtocol.Replay"/>).</summary>
        public IReadOnlyList<string> Shown
        {
            get { lock (gate) return [.. shown]; }
        }

        /// <summary>What the panel draws of a call in place of the result the model got, once.</summary>
        public JsonElement? TakeShown(string toolCallId)
        {
            lock (gate) return shownByCall.Remove(toolCallId, out var value) ? value : null;
        }

        public override ValueTask<bool> Show(string toolName, JsonElement? arguments, JsonElement? shown, string replay, CancellationToken cancellationToken)
        {
            lock (gate)
            {
                // The arguments are already on the call, as the model wrote them
                if (shown is { } value)
                {
                    shownByCall[CurrentCallId()] = value;
                }

                this.shown.Add(replay);
            }

            return ValueTask.FromResult(true);
        }

        public override ValueTask<bool> Suggest(string[] suggestions, CancellationToken cancellationToken)
            => ValueTask.FromResult(events.TryWrite(new BitAiFollowUpsEvent(suggestions)));

        public override async Task<BitAiApprovalDecision> AskApproval(string toolName, string reason, CancellationToken cancellationToken)
        {
            var callId = CurrentCallId();

            try
            {
                // One that clears the user's files is confirmed every time, so the panel offers no "always allow"
                await events.WriteAsync(new BitAiToolApprovalRequestedEvent(callId) { Reason = reason, AllowApproveAlways = false }, cancellationToken);

                return await approvalBroker.WaitAsync(callId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return BitAiApprovalDecision.Undecided;
            }
        }
    }

    /// <summary>
    /// A voice call, which streams no answer: what a tool shows is sent to the panel over the user's SignalR connection,
    /// as the events a text answer would have streamed, and the panel answers an approval with the user's decision.
    /// Nothing is signed: what a voice call says is replayed as the user's words, like its transcripts.
    /// </summary>
    private sealed class VoiceCallUi(IHubContext<AppHub> hubContext, string? signalRConnectionId) : ChatbotUi
    {
        public override async ValueTask<bool> Show(string toolName, JsonElement? arguments, JsonElement? shown, string replay, CancellationToken cancellationToken)
        {
            var callId = CurrentCallId();

            return await Send(SharedAppMessages.SHOW_AI_CHAT_EVENTS,
                [
                    new BitAiToolCallStartedEvent(callId, toolName) { Arguments = arguments },
                    new BitAiToolResultEvent(callId, shown is { } value ? BitAiToolOutcome.Success(value) : BitAiToolOutcome.FromText(replay))
                ], cancellationToken) is not null;
        }

        public override async ValueTask<bool> Suggest(string[] suggestions, CancellationToken cancellationToken)
            => await Send(SharedAppMessages.SHOW_AI_CHAT_EVENTS, [new BitAiFollowUpsEvent(suggestions)], cancellationToken) is not null;

        public override async Task<BitAiApprovalDecision> AskApproval(string toolName, string reason, CancellationToken cancellationToken)
        {
            var callId = CurrentCallId();

            var decision = await Send(SharedAppMessages.AWAIT_AI_CHAT_DECISION,
                [
                    new BitAiToolCallStartedEvent(callId, toolName) { Arguments = JsonDocument.Parse("{}").RootElement },
                    new BitAiToolApprovalRequestedEvent(callId) { Reason = reason, AllowApproveAlways = false }
                ], cancellationToken);

            return Enum.TryParse<BitAiApprovalDecision>(decision, out var parsed) ? parsed : BitAiApprovalDecision.Undecided;
        }

        private async Task<string?> Send(string method, BitAiEvent[] events, CancellationToken cancellationToken)
        {
            if (signalRConnectionId is null) return null;

            try
            {
                return await hubContext.Clients.Client(signalRConnectionId)
                    .InvokeAsync<string?>(method, events.Select(evt => BitAiWireSerializer.Serialize(evt)).ToArray(), cancellationToken);
            }
            catch (Exception exp) when (exp is not OperationCanceledException)
            {
                // The tab went away, or answered with an error: the tool tells the model it couldn't show it
                return null;
            }
        }
    }
}
