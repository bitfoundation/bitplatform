namespace Bit.BlazorUI;

/// <summary>
/// The model has decided to call a tool. Arguments may still be streaming.
/// </summary>
public sealed record BitAiToolCallStartedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="toolCallId">Identifier for this call, unique within the conversation.</param>
    /// <param name="toolName">The tool being called.</param>
    public BitAiToolCallStartedEvent(string toolCallId, string toolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ToolCallId = toolCallId;
        ToolName = toolName;
    }

    /// <summary>Identifier for this call.</summary>
    public string ToolCallId { get; }

    /// <summary>The tool being called.</summary>
    public string ToolName { get; }

    /// <summary>
    /// Complete arguments, when the provider does not stream them incrementally. Supplying this
    /// makes a separate completion event unnecessary.
    /// </summary>
    public JsonElement? Arguments { get; init; }

    /// <inheritdoc />
    public override string Type => "tool_call.started";
}

/// <summary>
/// More of the raw JSON argument text for a call in progress.
/// </summary>
/// <remarks>
/// Deltas are appended verbatim and parsed only when the call completes, so a partial fragment such
/// as <c>{"query":"bla</c> is safe to emit. The UI shows the partial text as it arrives.
/// </remarks>
public sealed record BitAiToolCallDeltaEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="toolCallId">The call being built.</param>
    /// <param name="argumentsDelta">Raw JSON text to append.</param>
    public BitAiToolCallDeltaEvent(string toolCallId, string argumentsDelta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentNullException.ThrowIfNull(argumentsDelta);
        ToolCallId = toolCallId;
        ArgumentsDelta = argumentsDelta;
    }

    /// <summary>The call being built.</summary>
    public string ToolCallId { get; }

    /// <summary>Raw JSON text to append.</summary>
    public string ArgumentsDelta { get; }

    /// <inheritdoc />
    public override string Type => "tool_call.delta";
}

/// <summary>
/// Arguments are complete. The call is ready to execute or to request approval.
/// </summary>
public sealed record BitAiToolCallCompletedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="toolCallId">The completed call.</param>
    /// <param name="arguments">
    /// The parsed arguments. When omitted, the framework parses the accumulated delta text.
    /// </param>
    public BitAiToolCallCompletedEvent(string toolCallId, JsonElement? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ToolCallId = toolCallId;
        Arguments = arguments;
    }

    /// <summary>The completed call.</summary>
    public string ToolCallId { get; }

    /// <summary>The parsed arguments, when the transport supplies them.</summary>
    public JsonElement? Arguments { get; }

    /// <inheritdoc />
    public override string Type => "tool_call.completed";
}

/// <summary>
/// A running tool reported progress.
/// </summary>
public sealed record BitAiToolProgressEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="toolCallId">The running call.</param>
    /// <param name="progress">The progress report.</param>
    public BitAiToolProgressEvent(string toolCallId, BitAiToolProgress progress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ToolCallId = toolCallId;
        Progress = progress;
    }

    /// <summary>The running call.</summary>
    public string ToolCallId { get; }

    /// <summary>The progress report.</summary>
    public BitAiToolProgress Progress { get; }

    /// <inheritdoc />
    public override string Type => "tool_call.progress";
}

/// <summary>
/// A tool produced a result.
/// </summary>
public sealed record BitAiToolResultEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="toolCallId">The call being answered.</param>
    /// <param name="outcome">The result.</param>
    public BitAiToolResultEvent(string toolCallId, BitAiToolOutcome outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentNullException.ThrowIfNull(outcome);
        ToolCallId = toolCallId;
        Outcome = outcome;
    }

    /// <summary>The call being answered.</summary>
    public string ToolCallId { get; }

    /// <summary>The result.</summary>
    public BitAiToolOutcome Outcome { get; }

    /// <inheritdoc />
    public override string Type => "tool_call.result";
}

/// <summary>
/// Execution is blocked until a human approves or rejects the call.
/// </summary>
/// <remarks>
/// Emitted by the framework when the approval policy demands it, and emittable by a transport whose
/// server side enforces its own policy.
/// </remarks>
public sealed record BitAiToolApprovalRequestedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="toolCallId">The call awaiting a decision.</param>
    public BitAiToolApprovalRequestedEvent(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ToolCallId = toolCallId;
    }

    /// <summary>The call awaiting a decision.</summary>
    public string ToolCallId { get; }

    /// <summary>An explanation shown in the approval prompt.</summary>
    public string? Reason { get; init; }

    /// <inheritdoc />
    public override string Type => "tool_call.approval_requested";
}
