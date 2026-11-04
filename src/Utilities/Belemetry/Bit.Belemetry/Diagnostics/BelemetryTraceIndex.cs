namespace Bit.Belemetry;

/// <summary>
/// Server only: recent server-side events no circuit could be attributed to (request spans, errors logged while a
/// request ran), indexed by trace id, so a capture can add the server's side of a request its own page made - the
/// trustworthy association being the trace id the page generated and propagated. Bounded: the least recently written
/// traces go first.
/// </summary>
internal sealed class BelemetryTraceIndex(int maxTraces = 1000, int maxEventsPerTrace = 16)
{
    private readonly object _lock = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _traces = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _order = new();

    public void Add(BelemetryDiagnosticEvent e)
    {
        if (e.TraceId is not { Length: 32 } traceId) return;

        lock (_lock)
        {
            if (_traces.TryGetValue(traceId, out var node))
            {
                _order.Remove(node);
                _order.AddLast(node);
            }
            else
            {
                if (_traces.Count >= maxTraces && _order.First is { } oldest)
                {
                    _traces.Remove(oldest.Value.TraceId);
                    _order.RemoveFirst();
                }
                node = _order.AddLast(new Entry(traceId));
                _traces.Add(traceId, node);
            }

            var events = node.Value.Events;
            if (events.Count >= maxEventsPerTrace) events.RemoveAt(0);
            events.Add(e);
        }
    }

    /// <summary>The events of the given traces.</summary>
    public List<BelemetryDiagnosticEvent> Find(IEnumerable<string> traceIds)
    {
        var found = new List<BelemetryDiagnosticEvent>();
        lock (_lock)
        {
            foreach (var traceId in traceIds.Distinct(StringComparer.Ordinal))
            {
                if (_traces.TryGetValue(traceId, out var node)) found.AddRange(node.Value.Events);
            }
        }
        return found;
    }

    private sealed record Entry(string TraceId)
    {
        public List<BelemetryDiagnosticEvent> Events { get; } = [];
    }
}
