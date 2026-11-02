using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A tool call and everything that happens to it: streaming arguments, an approval decision,
/// progress, and finally a result.
/// </summary>
/// <remarks>
/// <para>
/// The call and its result live in one part rather than two, because a tool call is one thing to a
/// reader: a card that fills in, runs, and resolves. Providers that emit results as separate tool
/// messages are reconciled onto the originating call by <see cref="ToolCallId"/>.
/// </para>
/// <para>
/// Arguments arrive as raw JSON fragments and are parsed leniently on every delta, so the UI can
/// show <c>city: "Sea…"</c> while the model is still writing rather than an opaque spinner.
/// </para>
/// </remarks>
public sealed class BitAiToolCallPart : BitAiMessagePart, IBitAiStreamablePart
{
    // The builder and the lazily parsed view are only touched under _gate: arguments stream in on the
    // transport's thread while the card reads them during render.
    private readonly StringBuilder _rawArguments = new(128);
    private readonly object _gate = new();
    private JsonElement? _arguments;
    private JsonElement? _partialArguments;
    private bool _partialArgumentsStale = true;

    /// <summary>Creates a tool call part.</summary>
    /// <param name="toolCallId">The call identifier used by the provider.</param>
    /// <param name="toolName">The tool being called.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiToolCallPart(string toolCallId, string toolName, string? partId = null) : base(partId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ToolCallId = toolCallId;
        ToolName = toolName;
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.ToolCall;

    /// <summary>The call identifier used by the provider.</summary>
    public string ToolCallId { get; }

    /// <summary>The tool being called.</summary>
    public string ToolName { get; }

    /// <summary>
    /// The tool definition, resolved from the registry when the call starts. Null for a tool the
    /// host does not know about, which the UI renders with the raw name.
    /// </summary>
    public BitAiToolDefinition? Definition { get; set; }

    /// <summary>Where the call is in its lifecycle.</summary>
    public BitAiToolCallStatus Status { get; private set; } = BitAiToolCallStatus.Streaming;

    /// <inheritdoc />
    public bool IsStreaming => Status is BitAiToolCallStatus.Streaming;

    /// <summary>The raw JSON argument text received so far.</summary>
    public string RawArguments
    {
        get
        {
            lock (_gate) return _rawArguments.ToString();
        }
    }

    /// <summary>The parsed arguments, available once the call completes.</summary>
    public JsonElement? Arguments
    {
        get
        {
            lock (_gate) return _arguments;
        }
    }

    /// <summary>The human decision on this call, when approval was required.</summary>
    public BitAiApprovalDecision Approval { get; private set; } = BitAiApprovalDecision.Undecided;

    /// <summary>Why approval was requested, shown in the prompt.</summary>
    public string? ApprovalReason { get; set; }

    /// <summary>The most recent progress report from the running tool.</summary>
    public BitAiToolProgress? Progress { get; private set; }

    /// <summary>The result, once the tool has produced one.</summary>
    public BitAiToolOutcome? Result { get; private set; }

    /// <summary>When execution began.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>When execution finished.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>How long execution took.</summary>
    public TimeSpan? Duration => Result?.Duration
        ?? (StartedAt is { } s && CompletedAt is { } c ? c - s : null);

    /// <summary>
    /// The best available view of the arguments: the parsed value once complete, otherwise a lenient
    /// parse of the partial JSON received so far.
    /// </summary>
    /// <remarks>
    /// Recomputed at most once per change, and only when read, so a renderer that ignores partial
    /// arguments pays nothing for them.
    /// </remarks>
    public JsonElement? DisplayArguments
    {
        get
        {
            lock (_gate)
            {
                if (_arguments is not null) return _arguments;
                if (!_partialArgumentsStale) return _partialArguments;
                _partialArguments = BitAiPartialJson.TryParse(_rawArguments.ToString());
                _partialArgumentsStale = false;
                return _partialArguments;
            }
        }
    }

    /// <summary>Appends raw JSON argument text.</summary>
    /// <param name="delta">The fragment to append.</param>
    public void AppendArguments(string? delta)
    {
        if (string.IsNullOrEmpty(delta)) return;
        lock (_gate)
        {
            _rawArguments.Append(delta);
            _partialArgumentsStale = true;
        }

        NotifyChanged();
    }

    /// <summary>
    /// Marks the arguments complete, parsing the accumulated text when none is supplied.
    /// </summary>
    /// <param name="arguments">
    /// The parsed arguments, or <see langword="null"/> to parse the accumulated raw text.
    /// </param>
    public void CompleteArguments(JsonElement? arguments = null)
    {
        lock (_gate)
        {
            _arguments = arguments ?? BitAiPartialJson.TryParseComplete(_rawArguments.ToString()) ?? EmptyObject;
            if (arguments is { } supplied && _rawArguments.Length == 0) _rawArguments.Append(supplied.GetRawText());
        }

        if (Status is BitAiToolCallStatus.Streaming) Status = BitAiToolCallStatus.Pending;
        NotifyChanged();
    }

    /// <summary>Moves the call to the given status.</summary>
    /// <param name="status">The new status.</param>
    public void SetStatus(BitAiToolCallStatus status)
    {
        if (Status == status) return;
        Status = status;
        if (status is BitAiToolCallStatus.Running) StartedAt ??= DateTimeOffset.UtcNow;
        NotifyChanged();
    }

    /// <summary>Records a human decision and moves the call forward or rejects it.</summary>
    /// <param name="decision">The decision.</param>
    public void SetApproval(BitAiApprovalDecision decision)
    {
        Approval = decision;
        Status = decision switch
        {
            BitAiApprovalDecision.Rejected => BitAiToolCallStatus.Rejected,
            BitAiApprovalDecision.Approved or BitAiApprovalDecision.ApprovedAlways => BitAiToolCallStatus.Pending,
            _ => Status,
        };
        NotifyChanged();
    }

    /// <summary>Records a progress report from the running tool.</summary>
    /// <param name="progress">The report.</param>
    public void SetProgress(BitAiToolProgress progress)
    {
        Progress = progress;
        NotifyChanged();
    }

    /// <summary>Records the result and moves the call to a terminal status.</summary>
    /// <param name="outcome">The result.</param>
    public void SetResult(BitAiToolOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        Result = outcome;
        CompletedAt = DateTimeOffset.UtcNow;
        Status = outcome.IsSuccess ? BitAiToolCallStatus.Completed : BitAiToolCallStatus.Failed;
        Progress = null;
        NotifyChanged();
    }

    /// <summary>Rebuilds the call from persisted state. Used by the conversation serializer.</summary>
    internal void RestoreState(
        JsonElement? arguments,
        string? rawArguments,
        BitAiToolCallStatus status,
        BitAiApprovalDecision approval,
        BitAiToolOutcome? result,
        DateTimeOffset? startedAt,
        DateTimeOffset? completedAt)
    {
        lock (_gate)
        {
            _arguments = arguments;
            _rawArguments.Clear();
            _rawArguments.Append(rawArguments ?? arguments?.GetRawText() ?? string.Empty);
            _partialArgumentsStale = true;
        }

        Status = status;
        Approval = approval;
        Result = result;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        NotifyChanged();
    }

    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();
}
