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

    /// <summary>
    /// How the call is presented - its title, icon and risk - when the tool is one the transport runs (a server-side
    /// tool, with <see cref="BitAiChatSessionOptions.ExecuteTools"/> off), so the app has no registered tool to describe
    /// it. Put on the call as <see cref="BitAiToolCallPart.Definition"/>; <see langword="null"/> leaves the call to the
    /// tool registered under its name, if any.
    /// </summary>
    /// <remarks>
    /// Only presentation is read from it: a session never runs a call because of it, and never sends it to the model.
    /// The <see cref="BitAiToolDefinition.Name"/> should be <see cref="ToolName"/>. A call the session runs itself
    /// (<see cref="BitAiChatSessionOptions.ExecuteTools"/> on, and a tool of that name registered) is described by that
    /// tool instead, so the prompt to allow it shows the risk of what will run.
    /// </remarks>
    public BitAiToolDefinition? Definition { get; init; }

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
/// server side enforces its own policy. Such a transport implements <see cref="IBitAiApprovalTransport"/> as well, to
/// be sent the decision the user makes.
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

    /// <summary>
    /// Whether the prompt offers to allow every later call of the tool as well. Put on the call as
    /// <see cref="BitAiToolCallPart.AllowApproveAlways"/>; turn it off for a call that must be confirmed every time,
    /// such as one that deletes data. The default is <see langword="true"/>.
    /// </summary>
    public bool AllowApproveAlways { get; init; } = true;

    /// <inheritdoc />
    public override string Type => "tool_call.approval_requested";
}
