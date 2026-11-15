namespace Bit.BlazorUI;

/// <summary>
/// A human decision on a tool call that the transport's side holds for approval, on its way back to it.
/// </summary>
/// <param name="ToolCallId">The call decided on.</param>
/// <param name="ToolName">The tool the call is for.</param>
/// <param name="Decision">The decision.</param>
public sealed record BitAiApprovalResponse(string ToolCallId, string ToolName, BitAiApprovalDecision Decision)
{
    /// <summary>The conversation the call belongs to.</summary>
    public string? ConversationId { get; init; }

    /// <summary>The message the call belongs to.</summary>
    public string? MessageId { get; init; }
}

/// <summary>
/// Implemented, besides <see cref="IBitAiTransport"/>, by a transport whose server holds tool calls for approval itself,
/// so the decisions the user makes in the UI reach it.
/// </summary>
/// <remarks>
/// <para>
/// A server that runs its own tools (<see cref="BitAiChatSessionOptions.ExecuteTools"/> off) asks for a decision with a
/// <see cref="BitAiToolApprovalRequestedEvent"/>, and waits. The approval prompt hands the decision to
/// <see cref="BitAiChatSession.Approve"/>, which records it on the call and - for a call the session does not run itself
/// (tools are not executed by the session, or no tool of that name is registered with it) - sends it on through
/// <see cref="SendApprovalAsync"/>, so the server can go on, typically on the stream of the turn still in flight.
/// </para>
/// <para>
/// A call the session runs itself is decided locally and never sent on, since the server would otherwise run it too.
/// A send that fails ends the call as failed, with <see cref="BitAiStrings.ApprovalNotDelivered"/> as its error, and is
/// logged; a send is cancelled when the session is disposed. <see cref="BitAiChatSession.ApprovalDecided"/> reports
/// every decision too, for an app that delivers them some other way.
/// </para>
/// </remarks>
public interface IBitAiApprovalTransport
{
    /// <summary>
    /// Sends a decision on a call the transport's side is holding for approval.
    /// </summary>
    /// <param name="response">The decision.</param>
    /// <param name="cancellationToken">Cancelled when the session is disposed.</param>
    /// <returns>A task that completes once the decision has been delivered.</returns>
    ValueTask SendApprovalAsync(BitAiApprovalResponse response, CancellationToken cancellationToken = default);
}
