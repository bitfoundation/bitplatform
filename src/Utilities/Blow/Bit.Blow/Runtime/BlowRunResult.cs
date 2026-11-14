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

/// <summary>
/// Run states kept up to date from a run's events, ready for <c>BlowCanvas.RunStates</c>: hand
/// <see cref="Apply"/> to <see cref="BlowRunRequest.OnEvent"/> and re-render. It also follows which edges carried a
/// value, to highlight them.
/// </summary>
public sealed class BlowRunStates : IReadOnlyDictionary<string, BlowRunState>
{
    private readonly Dictionary<string, BlowRunState> _states = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeEdges = new(StringComparer.Ordinal);

    /// <summary>The ids of the edges a value went along so far.</summary>
    public IReadOnlySet<string> ActiveEdges => _activeEdges;

    /// <summary>Bumped on every change, for a component to tell whether it has anything new to draw.</summary>
    public int Version { get; private set; }

    /// <summary>Takes in one event. A run starting clears what the last one left.</summary>
    public void Apply(BlowRunEvent runEvent)
    {
        ArgumentNullException.ThrowIfNull(runEvent);
        switch (runEvent.Kind)
        {
            case BlowRunEventKind.RunStarted:
                _states.Clear();
                _activeEdges.Clear();
                break;
            case BlowRunEventKind.EdgeActivated when runEvent.EdgeId is not null:
                _activeEdges.Add(runEvent.EdgeId);
                break;
            case BlowRunEventKind.NodeProgress when runEvent.NodeId is not null:
                // A progress note keeps the status and replaces only the message, unless it says nothing.
                var status = _states.TryGetValue(runEvent.NodeId, out var current) ? current.Status : BlowRunStatus.Running;
                _states[runEvent.NodeId] = new BlowRunState(status, runEvent.Message ?? current.Message);
                break;
            default:
                if (runEvent is { NodeId: { } id, Status: { } s }) _states[id] = new BlowRunState(s, runEvent.Message);
                break;
        }
        Version++;
    }

    /// <summary>Takes in events in order.</summary>
    public void Replay(IEnumerable<BlowRunEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var e in events) Apply(e);
    }

    /// <summary>Forgets every state and active edge.</summary>
    public void Clear()
    {
        _states.Clear();
        _activeEdges.Clear();
        Version++;
    }

    /// <inheritdoc />
    public BlowRunState this[string key] => _states[key];

    /// <inheritdoc />
    public IEnumerable<string> Keys => _states.Keys;

    /// <inheritdoc />
    public IEnumerable<BlowRunState> Values => _states.Values;

    /// <inheritdoc />
    public int Count => _states.Count;

    /// <inheritdoc />
    public bool ContainsKey(string key) => _states.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, out BlowRunState value) => _states.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, BlowRunState>> GetEnumerator() => _states.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>The messages a runner gives nodes it skips, stops or times out. Replace them to translate.</summary>
public sealed class BlowRunMessages
{
    /// <summary>A node skipped because something before it failed.</summary>
    public string UpstreamFailed { get; set; } = "An earlier step failed.";

    /// <summary>A node skipped because no incoming edge carried a value (<see cref="BlowJoinMode.Any"/>).</summary>
    public string NoInput { get; set; } = "No input reached it.";

    /// <summary>A node skipped because some incoming edge carried nothing (<see cref="BlowJoinMode.All"/>).</summary>
    public string MissingInput { get; set; } = "Not every input reached it.";

    /// <summary>A node skipped because the run stopped before reaching it.</summary>
    public string Stopped { get; set; } = "The run stopped before reaching it.";

    /// <summary>A node that was working when the run was cancelled.</summary>
    public string Cancelled { get; set; } = "Cancelled.";

    /// <summary>A node skipped because it has no handler (<see cref="BlowMissingHandlerBehavior.Skip"/>).</summary>
    public string NoHandler { get; set; } = "Nothing runs this kind of step.";

    /// <summary>A failure whose exception is not meant to be shown (see <see cref="BlowRunnerOptions.DescribeError"/>).</summary>
    public string UnexpectedError { get; set; } = "Failed.";

    /// <summary>An attempt that took longer than its timeout, given the timeout.</summary>
    public Func<TimeSpan, string> TimedOut { get; set; } = t => $"Timed out after {Format(t)}.";

    /// <summary>A node that failed because the run made more attempts than <see cref="BlowRunnerOptions.MaxExecutions"/>.</summary>
    public string TooManyExecutions { get; set; } = "The run reached its limit of steps.";

    /// <summary>Retrying after a failure, given the failure's message and the next attempt.</summary>
    public Func<string, int, string> Retrying { get; set; } = (message, attempt) => $"{message.TrimEnd('.')}. Trying again (attempt {attempt}).";

    internal static string Format(TimeSpan t) => t.TotalSeconds >= 1
        ? $"{t.TotalSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} s"
        : $"{t.TotalMilliseconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture)} ms";
}
