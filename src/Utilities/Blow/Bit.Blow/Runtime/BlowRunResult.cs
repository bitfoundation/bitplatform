namespace Bit.Blow;

/// <summary>How a run went: its outcome, every node's record, the events in order, and what it refused over.</summary>
public sealed class BlowRunResult
{
    internal BlowRunResult(string runId, BlowRunOutcome outcome, DateTimeOffset startedAt, DateTimeOffset finishedAt,
        IReadOnlyDictionary<string, BlowNodeRunRecord> nodes, IReadOnlyList<BlowValidationIssue> problems,
        IReadOnlyList<BlowRunEvent> events, IReadOnlySet<string> activeEdges)
    {
        RunId = runId;
        Outcome = outcome;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        Nodes = nodes;
        Problems = problems;
        Events = events;
        ActiveEdges = activeEdges;
    }

    /// <summary>The run's id.</summary>
    public string RunId { get; }

    /// <summary>How it ended.</summary>
    public BlowRunOutcome Outcome { get; }

    /// <summary>True when every node that ran succeeded.</summary>
    public bool Succeeded => Outcome == BlowRunOutcome.Succeeded;

    /// <summary>When it started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>When it ended.</summary>
    public DateTimeOffset FinishedAt { get; }

    /// <summary>How long it took.</summary>
    public TimeSpan Duration => FinishedAt - StartedAt;

    /// <summary>The record of every node that took part, by id.</summary>
    public IReadOnlyDictionary<string, BlowNodeRunRecord> Nodes { get; }

    /// <summary>Why the run was refused, when <see cref="Outcome"/> is <see cref="BlowRunOutcome.Refused"/>; empty otherwise.</summary>
    public IReadOnlyList<BlowValidationIssue> Problems { get; }

    /// <summary>Everything that happened, in order - for a log, a timeline, or playing the run back (<see cref="StatesAt"/>).</summary>
    public IReadOnlyList<BlowRunEvent> Events { get; }

    /// <summary>The ids of the edges a value went along.</summary>
    public IReadOnlySet<string> ActiveEdges { get; }

    /// <summary>What the node's handler produced, or null when it did not succeed.</summary>
    public BlowNodeResult? ResultOf(string nodeId) => Nodes.GetValueOrDefault(nodeId)?.Result;

    /// <summary>
    /// The value the node passed on: what <see cref="BlowNodeResult.Done"/> was given, or what its unnamed output
    /// carried. The default when it did not succeed or the value is not a <typeparamref name="T"/>.
    /// </summary>
    public T? ValueOf<T>(string nodeId, string? handleId = null)
        => ResultOf(nodeId) is { } r && r.TryGetOutput(handleId, out var v) && v is T value ? value : default;

    /// <summary>Every node's final state, for <c>BlowCanvas.RunStates</c>.</summary>
    public Dictionary<string, BlowRunState> ToRunStates()
        => Nodes.ToDictionary(n => n.Key, n => new BlowRunState(n.Value.Status, n.Value.Message), StringComparer.Ordinal);

    /// <summary>The states as they were after the first <paramref name="eventCount"/> events - to play the run back step by step.</summary>
    public BlowRunStates StatesAt(int eventCount)
    {
        var states = new BlowRunStates();
        states.Replay(Events.Take(Math.Clamp(eventCount, 0, Events.Count)));
        return states;
    }

    /// <summary>The results of the nodes that succeeded, for a later run to continue from (<see cref="BlowRunRequest.Resume"/>).</summary>
    public BlowRunCheckpoint ToCheckpoint() => new(RunId, Nodes
        .Where(n => n.Value is { Status: BlowRunStatus.Succeeded, Result: not null })
        .ToDictionary(n => n.Key, n => n.Value.Result!, StringComparer.Ordinal));
}
