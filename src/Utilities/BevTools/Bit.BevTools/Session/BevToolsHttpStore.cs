namespace Bit.BevTools;

internal sealed class BevToolsHttpStore
{
    private readonly BevToolsSession _session;
    private readonly BevToolsRingBuffer<BevToolsHttpRecord> _requests;
    private readonly object _lock = new();
    private long _nextId;
    private int _failed;
    private int _generation;

    public BevToolsHttpStore(BevToolsSession session, BevToolsOptions options)
    {
        _session = session;
        _requests = new BevToolsRingBuffer<BevToolsHttpRecord>(options.MaxHttpRequests);
    }

    public int Count => _requests.Count;

    public int FailedCount => Volatile.Read(ref _failed);

    public long NextId() => Interlocked.Increment(ref _nextId);

    public void Add(BevToolsHttpRecord record)
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

    public void MarkFailed(BevToolsHttpRecord record)
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

    public BevToolsHttpRecord[] Snapshot() => _requests.ToArray();

    public BevToolsHttpRecord? Find(long id) => _requests.FindByKey(static r => r.Id, id);

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
