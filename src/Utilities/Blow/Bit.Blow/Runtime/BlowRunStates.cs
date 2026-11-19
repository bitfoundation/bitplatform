namespace Bit.Blow;

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
