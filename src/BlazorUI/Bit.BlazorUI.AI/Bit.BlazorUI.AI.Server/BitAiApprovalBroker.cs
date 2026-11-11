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
/// Register it as a singleton (<c>services.AddSingleton&lt;BitAiApprovalBroker&gt;()</c>). The decisions are kept in the
/// memory of the server process, so a server running on several instances routes the approval to the one streaming
/// the call (sticky sessions, or a SignalR backplane where the hub's own connection carries it). A decision is matched
/// by the tool call's id alone, which the browser streaming the call is the one to know; keep the approval endpoint
/// behind the same authorization as the stream.
/// </para>
/// </remarks>
public sealed class BitAiApprovalBroker
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BitAiApprovalDecision>> _pending = new(StringComparer.Ordinal);

    /// <summary>The number of calls waiting for a decision.</summary>
    public int PendingCount => _pending.Count;

    /// <summary>
    /// Waits for the decision on a tool call.
    /// </summary>
    /// <param name="toolCallId">The call, as streamed in its <see cref="BitAiToolApprovalRequestedEvent"/>.</param>
    /// <param name="cancellationToken">
    /// Stops waiting - pass the stream's, so a reply the user stopped (or a closed browser) leaves nothing behind.
    /// </param>
    /// <returns>The decision.</returns>
    /// <exception cref="InvalidOperationException">The call is already waiting for a decision.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled first.</exception>
    public async Task<BitAiApprovalDecision> WaitAsync(string toolCallId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);

        var waiter = new TaskCompletionSource<BitAiApprovalDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(toolCallId, waiter))
        {
            throw new InvalidOperationException($"The tool call '{toolCallId}' is already waiting for a decision.");
        }

        try
        {
            using var registration = cancellationToken.Register(static state => ((TaskCompletionSource<BitAiApprovalDecision>)state!).TrySetCanceled(), waiter);
            return await waiter.Task.ConfigureAwait(false);
        }
        finally
        {
            _pending.TryRemove(new KeyValuePair<string, TaskCompletionSource<BitAiApprovalDecision>>(toolCallId, waiter));
        }
    }

    /// <summary>
    /// Hands a decision to the stream waiting for it.
    /// </summary>
    /// <param name="response">The decision, as the browser sent it.</param>
    /// <returns>
    /// <see langword="true"/> when a stream was waiting for it; <see langword="false"/> when no call of that id is waiting
    /// (it was decided already, its stream ended, or it is not known here).
    /// </returns>
    public bool TrySubmit(BitAiApprovalResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return _pending.TryRemove(response.ToolCallId, out var waiter) && waiter.TrySetResult(response.Decision);
    }
}
