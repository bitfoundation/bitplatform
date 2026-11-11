using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// Connects a server that holds a tool call for a human decision with the decision the browser sends back
/// (<see cref="IBitAiApprovalTransport"/>): the stream waits on <see cref="WaitAsync"/>, and the approval endpoint
/// (<see cref="BitAiSseEndpointRouteBuilderExtensions.MapBitAiApprovals(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string)"/>), the
/// WebSocket endpoint (<see cref="BitAiWebSocketEndpointRouteBuilderExtensions.MapBitAiWebSocket"/>)
/// or hub (<see cref="BitAiHub"/>) hands the decision to <see cref="TrySubmit"/>.
/// </summary>
/// <remarks>
/// <para>
/// A server streams a <see cref="BitAiToolApprovalRequestedEvent"/> for the call, then awaits the decision before it runs
/// the tool (or not), all in the same stream:
/// </para>
/// <code>
/// yield return new BitAiToolCallStartedEvent(callId, "delete_file") { Arguments = arguments };
/// yield return new BitAiToolApprovalRequestedEvent(callId);
/// var decision = await approvals.WaitAsync(callId, cancellationToken);
/// </code>
/// <para>
/// The package's endpoints and hub tell the broker about the call as they send its
/// <see cref="BitAiToolApprovalRequestedEvent"/>, so a decision that reaches the server before the stream gets to
/// <see cref="WaitAsync"/> (the browser answering at once, or a decision the user made before the request arrived) is
/// kept for it rather than lost. A decision on a call nothing asked about is refused.
/// </para>
/// <para>
/// Register it as a singleton (<c>services.AddSingleton&lt;BitAiApprovalBroker&gt;()</c>). The decisions are kept in the
/// memory of the server process, so a server running on several instances routes the approval to the one streaming
/// the call (sticky sessions, or a SignalR backplane where the hub's own connection carries it). A decision is matched
/// by the tool call's id alone, which the browser streaming the call is the one to know; keep the approval endpoint
/// behind the same authorization as the stream.
/// </para>
/// </remarks>
public sealed class BitAiApprovalBroker
{
    private readonly ConcurrentDictionary<string, Call> _calls = new(StringComparer.Ordinal);

    /// <summary>The number of calls waiting for a decision.</summary>
    public int PendingCount => _calls.Values.Count(c => c.IsWaited);

    /// <summary>
    /// Waits for the decision on a tool call.
    /// </summary>
    /// <param name="toolCallId">The call, as streamed in its <see cref="BitAiToolApprovalRequestedEvent"/>.</param>
    /// <param name="cancellationToken">
    /// Stops waiting - pass the stream's, so a reply the user stopped (or a closed browser) leaves nothing behind.
    /// </param>
    /// <returns>The decision, at once when it arrived before the wait started.</returns>
    /// <exception cref="InvalidOperationException">The call is already waiting for a decision.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled first.</exception>
    public async Task<BitAiApprovalDecision> WaitAsync(string toolCallId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);

        var call = _calls.GetOrAdd(toolCallId, static _ => new Call());
        if (!call.StartWaiting())
        {
            throw new InvalidOperationException($"The tool call '{toolCallId}' is already waiting for a decision.");
        }

        try
        {
            using var registration = cancellationToken.Register(static state => ((Call)state!).Decision.TrySetCanceled(), call);
            return await call.Decision.Task.ConfigureAwait(false);
        }
        finally
        {
            _calls.TryRemove(new KeyValuePair<string, Call>(toolCallId, call));
        }
    }

    /// <summary>
    /// Hands a decision to the stream waiting for it, or keeps it for the stream about to wait for it.
    /// </summary>
    /// <param name="response">The decision, as the browser sent it.</param>
    /// <returns>
    /// <see langword="true"/> when the call was waiting for it, or was asked about and is about to; <see langword="false"/>
    /// when no call of that id is expecting one (it was decided already, its stream ended, or it is not known here).
    /// </returns>
    public bool TrySubmit(BitAiApprovalResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return _calls.TryGetValue(response.ToolCallId, out var call) && call.Decision.TrySetResult(response.Decision);
    }

    /// <summary>
    /// Records that a stream is asking about a call, before its <see cref="BitAiToolApprovalRequestedEvent"/> is sent, so
    /// a decision arriving before the stream waits for it is kept.
    /// </summary>
    internal void Expect(string toolCallId)
    {
        if (!string.IsNullOrWhiteSpace(toolCallId)) _calls.TryAdd(toolCallId, new Call());
    }

    /// <summary>
    /// Forgets a call a stream asked about once that stream has ended, unless something is still waiting on it.
    /// </summary>
    internal void Forget(string toolCallId)
    {
        if (_calls.TryGetValue(toolCallId, out var call) && !call.IsWaited)
        {
            _calls.TryRemove(new KeyValuePair<string, Call>(toolCallId, call));
        }
    }

    private sealed class Call
    {
        private int _waited;

        public TaskCompletionSource<BitAiApprovalDecision> Decision { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsWaited => Volatile.Read(ref _waited) == 1;

        public bool StartWaiting() => Interlocked.Exchange(ref _waited, 1) == 0;
    }
}
