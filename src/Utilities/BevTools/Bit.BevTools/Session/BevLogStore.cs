using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>The session's log entries: the newest <see cref="BitBevToolsOptions.MaxLogs"/>, with session-long counts per level.</summary>
internal sealed class BevLogStore
{
    private readonly BevToolsSession _session;
    private readonly RingBuffer<BevLogRecord> _records;
    // Per LogLevel (Trace … Critical), counted for the whole session: the buffer drops the oldest entries, the counts
    // still say how many warnings there were.
    private readonly int[] _counts = new int[6];
    private long _nextId;

    public BevLogStore(BevToolsSession session, BitBevToolsOptions options)
    {
        _session = session;
        _records = new RingBuffer<BevLogRecord>(options.MaxLogs);
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

    public void Add(BevLogRecord record)
    {
        _records.Add(record);
        if (record.Level is >= LogLevel.Trace and <= LogLevel.Critical)
        {
            Interlocked.Increment(ref _counts[(int)record.Level]);
        }

        _session.Touch();
    }

    public BevLogRecord[] Snapshot() => _records.ToArray();

    public BevLogRecord? Find(long id) => _records.FindByKey(static r => r.Id, id);

    public void Clear()
    {
        _records.Clear();
        for (var i = 0; i < _counts.Length; i++)
        {
            Volatile.Write(ref _counts[i], 0);
        }

        _session.Touch();
    }
}
