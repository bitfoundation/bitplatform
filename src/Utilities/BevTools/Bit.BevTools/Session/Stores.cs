using System.Collections.Concurrent;

namespace Bit.BevTools;

internal sealed class HttpStore
{
    private readonly BevToolsSession _session;
    private readonly RingBuffer<BevHttpRecord> _requests;
    private readonly object _lock = new();
    private long _nextId;
    private int _failed;
    private int _generation;

    public HttpStore(BevToolsSession session, BitBevToolsOptions options)
    {
        _session = session;
        _requests = new RingBuffer<BevHttpRecord>(options.MaxHttpRequests);
    }

    public int Count => _requests.Count;

    public int FailedCount => Volatile.Read(ref _failed);

    public long NextId() => Interlocked.Increment(ref _nextId);

    public void Add(BevHttpRecord record)
    {
        lock (_lock)
        {
            record.StoreGeneration = _generation;
            if (_requests.Add(record, out var evicted) && evicted?.FailureCounted == true)
            {
                _failed--;
            }
        }

        _session.Touch();
    }

    public void MarkFailed(BevHttpRecord record)
    {
        lock (_lock)
        {
            if (!record.FailureCounted
                && record.StoreGeneration == _generation
                && ReferenceEquals(_requests.FindByKey(static r => r.Id, record.Id), record))
            {
                record.FailureCounted = true;
                _failed++;
            }
        }
    }

    public BevHttpRecord[] Snapshot() => _requests.ToArray();

    public BevHttpRecord? Find(long id) => _requests.FindByKey(static r => r.Id, id);

    public void Clear()
    {
        lock (_lock)
        {
            _generation++;
            _requests.Clear();
            _failed = 0;
        }

        _session.Touch();
    }

    public void Touch() => _session.Touch();
}

public sealed class BevJsInteropAggregate
{
    public required string Identifier { get; init; }

    public int Count;

    public double TotalMs;

    public double MaxMs;

    public int Failures;

    public BevJsInteropDirection Direction { get; init; }
}

internal sealed class JsInteropStore
{
    private readonly BevToolsSession _session;
    private readonly RingBuffer<BevJsInteropRecord> _calls;
    private readonly ConcurrentDictionary<string, BevJsInteropAggregate> _aggregates = new(StringComparer.Ordinal);
    private long _nextId;

    public JsInteropStore(BevToolsSession session, BitBevToolsOptions options)
    {
        _session = session;
        _calls = new RingBuffer<BevJsInteropRecord>(options.MaxJsInteropCalls);
    }

    public int Count => _calls.Count;

    public long NextId() => Interlocked.Increment(ref _nextId);

    public void Add(BevJsInteropRecord record)
    {
        _calls.Add(record);
        _session.Touch();
    }

    public void Complete(BevJsInteropRecord record, double durationMs, bool succeeded, string? error)
    {
        record.DurationMs = durationMs;
        record.Succeeded = succeeded;
        record.Error = error;
        var key = (record.Direction == BevJsInteropDirection.JsToDotNet ? "←" : "→") + record.Identifier;
        var agg = _aggregates.GetOrAdd(key, _ => new BevJsInteropAggregate { Identifier = record.Identifier, Direction = record.Direction });
        lock (agg)
        {
            agg.Count++;
            agg.TotalMs += durationMs;
            if (durationMs > agg.MaxMs)
            {
                agg.MaxMs = durationMs;
            }

            if (!succeeded)
            {
                agg.Failures++;
            }
        }

        _session.Touch();
    }

    public BevJsInteropRecord[] Snapshot() => _calls.ToArray();

    public BevJsInteropAggregate[] Aggregates() => _aggregates.Values.ToArray();

    public void Clear()
    {
        _calls.Clear();
        _aggregates.Clear();
        _session.Touch();
    }
}
