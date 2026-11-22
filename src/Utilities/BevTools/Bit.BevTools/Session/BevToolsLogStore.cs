using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>The session's log entries: the newest <see cref="BevToolsOptions.MaxLogs"/>, with session-long counts per level.</summary>
internal sealed class BevToolsLogStore
{
    private readonly BevToolsSession _session;
    private readonly BevToolsRingBuffer<BevToolsLogRecord> _records;
    // Per LogLevel (Trace … Critical), counted for the whole session: the buffer drops the oldest entries, the counts
    // still say how many warnings there were.
    private readonly int[] _counts = new int[6];
    // An entry and its count go in together, and a clear takes both away together: a Clear between the two would leave
    // a count for an entry that is gone, or an entry without its count.
    private readonly object _lock = new();
    private long _nextId;

    public BevToolsLogStore(BevToolsSession session, BevToolsOptions options)
    {
        _session = session;
        _records = new BevToolsRingBuffer<BevToolsLogRecord>(options.MaxLogs);
    }

    public int Count => _records.Count;

    public int Total
    {
        get
        {
            var total = 0;
            for (var i = 0; i < _counts.Length; i++)
            {
                total += Volatile.Read(ref _counts[i]);
            }

            return total;
        }
    }

    /// <summary>How many entries of this level were logged this session, the ones the buffer no longer holds included.</summary>
    public int CountOf(LogLevel level) => level is >= LogLevel.Trace and <= LogLevel.Critical ? Volatile.Read(ref _counts[(int)level]) : 0;

    public long NextId() => Interlocked.Increment(ref _nextId);

    public void Add(BevToolsLogRecord record)
    {
        lock (_lock)
        {
            _records.Add(record);
            if (record.Level is >= LogLevel.Trace and <= LogLevel.Critical)
            {
                Interlocked.Increment(ref _counts[(int)record.Level]);
            }
        }

        _session.Touch();
    }

    /// <summary>
    /// Reserves ids for entries added later by <see cref="Import"/>, so they sort before whatever is recorded meanwhile.
    /// Returns the first; the others follow it.
    /// </summary>
    public long ReserveIds(int count) => Interlocked.Add(ref _nextId, count) - count + 1;

    /// <summary>
    /// Adds the entries Bit.BevTools.Loader kept before the session was attached, with ids from <see cref="ReserveIds"/>,
    /// and the counts per level of every entry it captured: the ones it no longer held count too, as they do here. The
    /// buffer is kept in id order, which lookups by id rely on, so what the session recorded since the ids were reserved
    /// stays after them.
    /// </summary>
    public void Import(IReadOnlyList<BevToolsLogRecord> records, IReadOnlyList<int> counts)
    {
        lock (_lock)
        {
            // One step of the ring's own: Snapshot and Find read it without this lock, and must never see it half rebuilt.
            _records.Replace(current => current.Concat(records).OrderBy(static r => r.Id));

            for (var i = 0; i < _counts.Length && i < counts.Count; i++)
            {
                Interlocked.Add(ref _counts[i], counts[i]);
            }
        }

        _session.Touch();
    }

    public BevToolsLogRecord[] Snapshot() => _records.ToArray();

    public BevToolsLogRecord? Find(long id) => _records.FindByKey(static r => r.Id, id);

    public void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
            for (var i = 0; i < _counts.Length; i++)
            {
                Volatile.Write(ref _counts[i], 0);
            }
        }

        _session.Touch();
    }
}
