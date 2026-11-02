namespace Bit.BlazorUI;

/// <summary>
/// One step of an agent run.
/// </summary>
/// <remarks>
/// A step is the unit the user reasons about: "search the docs", "write the migration". It is not
/// the same as a tool call, though a step usually contains one or more.
/// </remarks>
public sealed class BitAiAgentStep
{
    // Copy-on-write, like the steps of a run: the timeline reads the list on the renderer's thread
    // while calls are recorded on the transport's.
    private string[] _toolCallIds = [];
    private int _version;

    /// <summary>Creates a step.</summary>
    /// <param name="stepId">Stable identifier, preserved across plan revisions.</param>
    /// <param name="title">What the step does.</param>
    public BitAiAgentStep(string stepId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
        ArgumentNullException.ThrowIfNull(title);
        StepId = stepId;
        Title = title;
    }

    /// <summary>Stable identifier, preserved across plan revisions.</summary>
    public string StepId { get; }

    /// <summary>What the step does.</summary>
    public string Title { get; internal set; }

    /// <summary>Elaboration shown when the step is expanded.</summary>
    public string? Detail { get; set; }

    /// <summary>Where the step is in its lifecycle.</summary>
    public BitAiAgentStepStatus Status { get; private set; } = BitAiAgentStepStatus.Pending;

    /// <summary>A short account of what the step produced.</summary>
    public string? Summary { get; private set; }

    /// <summary>The failure, when the step failed.</summary>
    public BitAiError? Error { get; private set; }

    /// <summary>When the step started.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>When the step finished.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>How long the step took.</summary>
    public TimeSpan? Duration => StartedAt is { } s && CompletedAt is { } c ? c - s : null;

    /// <summary>Identifiers of the tool calls made during this step.</summary>
    public IReadOnlyList<string> ToolCallIds => Volatile.Read(ref _toolCallIds);

    /// <summary>Increments on every change, for cheap render checks.</summary>
    public int Version => _version;

    /// <summary>Raised after the step changes.</summary>
    public event Action<BitAiAgentStep>? Changed;

    /// <summary>Marks the step running.</summary>
    public void Start()
    {
        Status = BitAiAgentStepStatus.Running;
        StartedAt ??= DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Records a tool call made during this step. Recording the same call twice has no effect.</summary>
    /// <param name="toolCallId">The tool call identifier.</param>
    public void AddToolCall(string toolCallId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolCallId);

        // A compare-and-swap loop rather than a plain write, because tools run in parallel and two
        // may report into the same step at once.
        string[] current, updated;
        do
        {
            current = Volatile.Read(ref _toolCallIds);
            if (current.Contains(toolCallId, StringComparer.Ordinal)) return;
            updated = [.. current, toolCallId];
        }
        while (Interlocked.CompareExchange(ref _toolCallIds, updated, current) != current);

        Touch();
    }

    /// <summary>Marks the step blocked on the user.</summary>
    public void Block()
    {
        Status = BitAiAgentStepStatus.Blocked;
        Touch();
    }

    /// <summary>Marks the step finished.</summary>
    /// <param name="status">How it finished.</param>
    /// <param name="summary">A short account of what it produced.</param>
    /// <param name="error">The failure, when it failed.</param>
    public void Complete(BitAiAgentStepStatus status = BitAiAgentStepStatus.Completed, string? summary = null, BitAiError? error = null)
    {
        Status = status;
        Summary = summary ?? Summary;
        Error = error ?? Error;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Rebuilds the step from persisted state. Used by the conversation serializer.</summary>
    internal void RestoreState(BitAiAgentStepStatus status, string? summary, BitAiError? error, DateTimeOffset? startedAt, DateTimeOffset? completedAt, IEnumerable<string> toolCallIds)
    {
        Status = status;
        Summary = summary;
        Error = error;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Volatile.Write(ref _toolCallIds, [.. toolCallIds]);
        Touch();
    }

    private void Touch()
    {
        Interlocked.Increment(ref _version);
        Changed?.Invoke(this);
    }
}
