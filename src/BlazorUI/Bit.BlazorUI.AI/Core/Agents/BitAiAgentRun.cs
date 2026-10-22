using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A multi-step agent execution: a plan, the steps as they run, and the aggregate result.
/// </summary>
/// <remarks>
/// <para>
/// A run is what turns opaque agent activity into something a user can follow. It exists so the UI
/// can answer "what is it doing, what has it done, and what is left" without the application having
/// to invent its own bookkeeping.
/// </para>
/// <para>
/// Plans are replaced rather than patched, because agents routinely rewrite the tail of a plan as
/// they learn. Steps that keep their identifier across a revision keep their state and timings.
/// </para>
/// </remarks>
public sealed class BitAiAgentRun
{
    // Copy-on-write: the timeline enumerates the steps on the renderer's thread while the stream
    // revises the plan on the transport's.
    private volatile BitAiAgentStep[] _steps = [];

    // Boxed so a reader never sees a total that is half one update and half the next.
    private volatile StrongBox<BitAiTokenUsage> _usage = new();
    private int _version;

    /// <summary>Creates a run.</summary>
    /// <param name="runId">The identifier, or <see langword="null"/> to generate one.</param>
    /// <param name="agentName">The name of the agent, shown in the run header.</param>
    public BitAiAgentRun(string? runId = null, string? agentName = null)
    {
        RunId = runId ?? BitAiId.NewRunId();
        AgentName = agentName;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>The run identifier.</summary>
    public string RunId { get; }

    /// <summary>The name of the agent.</summary>
    public string? AgentName { get; set; }

    /// <summary>The goal the run is pursuing.</summary>
    public string? Goal { get; set; }

    /// <summary>Where the run is in its lifecycle.</summary>
    public BitAiAgentRunStatus Status { get; private set; } = BitAiAgentRunStatus.Idle;

    /// <summary>The steps, in execution order.</summary>
    public IReadOnlyList<BitAiAgentStep> Steps => _steps;

    /// <summary>The step currently running, when there is one.</summary>
    public BitAiAgentStep? CurrentStep => _steps.FirstOrDefault(s => s.Status is BitAiAgentStepStatus.Running)
        ?? _steps.FirstOrDefault(s => s.Status is BitAiAgentStepStatus.Blocked);

    /// <summary>What the agent is waiting for, when the run is blocked on the user.</summary>
    public string? PendingPrompt { get; private set; }

    /// <summary>Suggested answers to <see cref="PendingPrompt"/>.</summary>
    public IReadOnlyList<string>? PendingSuggestions { get; private set; }

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>When the run ended.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>How long the run took, or has taken so far.</summary>
    public TimeSpan Elapsed => (CompletedAt ?? DateTimeOffset.UtcNow) - StartedAt;

    /// <summary>Token usage of every model request made within the run.</summary>
    /// <remarks>
    /// Each request counts once, with the last usage its stream reported, so a run that called a
    /// model six times reports the sum of six final figures however many updates each one streamed.
    /// </remarks>
    public BitAiTokenUsage Usage => _usage.Value;

    /// <summary>The failure that ended the run, when it failed.</summary>
    public BitAiError? Error { get; private set; }

    /// <summary>How many steps have finished successfully.</summary>
    public int CompletedStepCount => _steps.Count(s => s.Status is BitAiAgentStepStatus.Completed);

    /// <summary>Completion in the range 0..1, or <see langword="null"/> when there is no plan.</summary>
    public double? Progress
    {
        get
        {
            var steps = _steps;
            return steps.Length == 0
                ? null
                : (double)steps.Count(s => s.Status is BitAiAgentStepStatus.Completed) / steps.Length;
        }
    }

    /// <summary>Increments on every change, for cheap render checks.</summary>
    public int Version => _version;

    /// <summary>Raised after the run changes.</summary>
    public event Action<BitAiAgentRun>? Changed;

    /// <summary>
    /// Replaces the plan, preserving the state of steps whose identifier survives the revision.
    /// </summary>
    /// <param name="steps">The new plan, in execution order.</param>
    public void SetPlan(IReadOnlyList<BitAiAgentPlanStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        // Model-written plans can repeat an identifier; the first occurrence wins rather than throwing.
        var existing = new Dictionary<string, BitAiAgentStep>(StringComparer.Ordinal);
        foreach (var step in _steps) existing.TryAdd(step.StepId, step);

        var placed = new HashSet<string>(StringComparer.Ordinal);
        var revised = new List<BitAiAgentStep>(steps.Count);

        foreach (var planned in steps)
        {
            if (!placed.Add(planned.StepId)) continue;

            if (existing.Remove(planned.StepId, out var kept))
            {
                kept.Title = planned.Title;
                kept.Detail = planned.Detail ?? kept.Detail;
                revised.Add(kept);
            }
            else
            {
                var step = new BitAiAgentStep(planned.StepId, planned.Title) { Detail = planned.Detail };
                step.Changed += OnStepChanged;
                revised.Add(step);
            }
        }

        _steps = [.. revised];
        foreach (var dropped in existing.Values) dropped.Changed -= OnStepChanged;

        if (Status is BitAiAgentRunStatus.Idle) Status = BitAiAgentRunStatus.Planning;
        Touch();
    }

    /// <summary>Finds a step, creating it when the agent starts one that was never planned.</summary>
    /// <param name="stepId">The step identifier.</param>
    /// <param name="title">The title to use when creating.</param>
    /// <returns>The step.</returns>
    public BitAiAgentStep GetOrAddStep(string stepId, string? title = null)
    {
        var step = _steps.FirstOrDefault(s => s.StepId == stepId);
        if (step is not null)
        {
            if (title is not null) step.Title = title;
            return step;
        }

        step = new BitAiAgentStep(stepId, title ?? stepId);
        step.Changed += OnStepChanged;
        _steps = [.. _steps, step];
        Touch();
        return step;
    }

    /// <summary>Moves the run to a new status.</summary>
    /// <param name="status">The new status.</param>
    public void SetStatus(BitAiAgentRunStatus status)
    {
        if (Status == status) return;
        Status = status;
        if (status is BitAiAgentRunStatus.Completed or BitAiAgentRunStatus.Failed or BitAiAgentRunStatus.Cancelled)
        {
            CompletedAt ??= DateTimeOffset.UtcNow;
            PendingPrompt = null;
            PendingSuggestions = null;
        }

        Touch();
    }

    /// <summary>Blocks the run until the user answers.</summary>
    /// <param name="prompt">What the agent needs.</param>
    /// <param name="suggestions">Suggested answers, rendered as one-tap choices.</param>
    public void RequestInput(string prompt, IReadOnlyList<string>? suggestions = null)
    {
        PendingPrompt = prompt;
        PendingSuggestions = suggestions;
        Status = BitAiAgentRunStatus.WaitingForInput;
        Touch();
    }

    /// <summary>Clears a pending input request and resumes running.</summary>
    public void ResolveInput()
    {
        PendingPrompt = null;
        PendingSuggestions = null;
        if (Status is BitAiAgentRunStatus.WaitingForInput) Status = BitAiAgentRunStatus.Running;
        Touch();
    }

    /// <summary>Adds the final token usage of one request to the run total.</summary>
    /// <param name="usage">The usage to add.</param>
    public void AddUsage(BitAiTokenUsage usage) => SetUsage(Usage + usage);

    /// <summary>Replaces the run total, for a stream whose cumulative usage supersedes its last report.</summary>
    /// <param name="usage">The new total.</param>
    internal void SetUsage(BitAiTokenUsage usage)
    {
        _usage = new StrongBox<BitAiTokenUsage>(usage);
        Touch();
    }

    /// <summary>Records the failure that ended the run.</summary>
    /// <param name="error">The failure.</param>
    public void SetError(BitAiError error)
    {
        Error = error;
        SetStatus(error.Kind is BitAiErrorKind.Cancelled ? BitAiAgentRunStatus.Cancelled : BitAiAgentRunStatus.Failed);
    }

    /// <remarks>
    /// Every step, planned or started ad hoc, reports through here, because the run header shows
    /// progress derived from step states.
    /// </remarks>
    private void OnStepChanged(BitAiAgentStep step) => Touch();

    /// <summary>Rebuilds the run from persisted state. Used by the conversation serializer.</summary>
    internal void RestoreState(
        BitAiAgentRunStatus status,
        string? goal,
        string? pendingPrompt,
        IReadOnlyList<string>? pendingSuggestions,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        BitAiTokenUsage usage,
        BitAiError? error,
        IEnumerable<BitAiAgentStep> steps)
    {
        Status = status;
        Goal = goal;
        PendingPrompt = pendingPrompt;
        PendingSuggestions = pendingSuggestions;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        _usage = new StrongBox<BitAiTokenUsage>(usage);
        Error = error;

        foreach (var existing in _steps) existing.Changed -= OnStepChanged;
        var restored = new List<BitAiAgentStep>();
        foreach (var step in steps)
        {
            step.Changed += OnStepChanged;
            restored.Add(step);
        }

        _steps = [.. restored];
        Touch();
    }

    private void Touch()
    {
        Interlocked.Increment(ref _version);
        Changed?.Invoke(this);
    }
}
