using System.Collections.Concurrent;

namespace Bit.BevTools;

internal sealed class BevToolsJsInteropStore
{
    private readonly BevToolsSession _session;
    private readonly BevToolsRingBuffer<BevToolsJsInteropRecord> _calls;
    private readonly ConcurrentDictionary<string, BevToolsJsInteropAggregate> _aggregates = new(StringComparer.Ordinal);
    private long _nextId;

    public BevToolsJsInteropStore(BevToolsSession session, BevToolsOptions options)
    {
        _session = session;
        _calls = new BevToolsRingBuffer<BevToolsJsInteropRecord>(options.MaxJsInteropCalls);
    }

    public int Count => _calls.Count;

    /// <summary>The calls the held records stand for: a record can stand for several (<see cref="BevToolsJsInteropRecord.CallCount"/>).</summary>
    public int CallCount
    {
        get
        {
            var calls = 0;
            foreach (var record in _calls.ToArray())
            {
                calls += record.CallCount;
            }

            return calls;
        }
    }

    public long NextId() => Interlocked.Increment(ref _nextId);

    public void Add(BevToolsJsInteropRecord record)
    {
        _calls.Add(record);
        _session.Touch();
    }

    /// <summary>
    /// Completes a record. <paramref name="durationMs"/> is the longest of the calls it stands for and
    /// <paramref name="totalMs"/> their sum (the same, for a record of one call); a failed record's calls all failed.
    /// </summary>
    public void Complete(BevToolsJsInteropRecord record, double durationMs, bool succeeded, string? error, double? totalMs = null)
    {
        var calls = Math.Max(1, record.CallCount);
        var total = calls == 1 ? durationMs : totalMs ?? durationMs;
        record.DurationMs = durationMs;
        record.TotalDurationMs = calls == 1 ? null : total;
        record.Succeeded = succeeded;
        record.Error = error;
        var key = (record.Direction == BevToolsJsInteropDirection.JsToDotNet ? "←" : "→") + record.Identifier;
        var agg = _aggregates.GetOrAdd(key, _ => new BevToolsJsInteropAggregate { Identifier = record.Identifier, Direction = record.Direction });
        lock (agg)
        {
            agg.Count += calls;
            agg.TotalMs += total;
            if (durationMs > agg.MaxMs)
            {
                agg.MaxMs = durationMs;
            }

            if (!succeeded)
            {
                agg.Failures += calls;
            }
        }

        _session.Touch();
    }

    public BevToolsJsInteropRecord[] Snapshot() => _calls.ToArray();

    public BevToolsJsInteropAggregate[] Aggregates() => _aggregates.Values.ToArray();

    public void Clear()
    {
        _calls.Clear();
        _aggregates.Clear();
        _session.Touch();
    }
}
