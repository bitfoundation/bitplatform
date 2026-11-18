namespace Bit.BevTools;

/// <summary>
/// The session's memory over time, read once a second while BevTools' panel component is on the page (open or not) and
/// kept while capture is on (paused, nothing is added): the
/// heap, full collections - each a timeline event, so a slow interaction or a long frame can be read next to the pause
/// that came with it - the heap after each full collection, and on WebAssembly the linear memory. The last ten minutes.
/// On a server the heap and its collections are the process's, shared by every circuit, and the panel says so.
/// </summary>
public sealed class BevToolsMemoryTrend
{
    private const int Capacity = 600;
    private readonly BevToolsRingBuffer<BevToolsMemorySample> _samples = new(Capacity);
    private readonly BevToolsRingBuffer<BevToolsFullCollection> _collections = new(100);
    private readonly object _lock = new();
    private BevToolsMemoryReading? _last;
    private DateTimeOffset _lastAt;

    public BevToolsMemorySample[] Samples => _samples.ToArray();

    /// <summary>The full collections seen while sampling, oldest first.</summary>
    public BevToolsFullCollection[] FullCollections => _collections.ToArray();

    internal void Clear()
    {
        lock (_lock)
        {
            // A trend that starts over: the next reading is taken whenever it comes, and a collection is told against
            // readings made after the clear only.
            _samples.Clear();
            _collections.Clear();
            _last = null;
            _lastAt = default;
        }
    }

    /// <summary>Reads the counters at most once a second; returns whether a sample was taken.</summary>
    internal bool Sample(BevToolsSession session, DateTimeOffset now, Func<BevToolsMemoryReading> read)
    {
        lock (_lock)
        {
            if (now - _lastAt < TimeSpan.FromSeconds(1))
            {
                return false;
            }

            _lastAt = now;
        }

        var reading = read();
        // Paused, the trend stays as it was read, like every other buffer; the counters are still followed, so a
        // collection made while paused is not put down to the first reading after it.
        if (session.IsCapturing)
        {
            _samples.Add(new BevToolsMemorySample(now, reading.HeapBytes, reading.FullCollections, reading.PauseMs) { LinearMemoryBytes = reading.LinearMemoryBytes });
        }

        BevToolsMemoryReading? previous;
        lock (_lock)
        {
            previous = _last;
            _last = reading;
        }

        if (previous is not { } before || reading.FullCollections <= before.FullCollections || !session.IsCapturing)
        {
            return true;
        }

        // The runtime's own record of its last full collection when it keeps one (CoreCLR), else the heap now.
        var exact = reading.LastFullIndex > before.LastFullIndex && reading.LastFullIndex > 0;
        var heapAfter = exact ? reading.LastFullHeapBytes : reading.HeapBytes;
        var pause = exact ? reading.LastFullPauseMs : null;
        var count = reading.FullCollections - before.FullCollections;
        _collections.Add(new BevToolsFullCollection(now, heapAfter, pause, exact));
        var paused = reading.PauseMs - before.PauseMs;
        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Custom,
            Category = "gc",
            Title = (count == 1 ? "Full garbage collection" : count + " full garbage collections") + ": heap " + BevToolsFmt.Bytes(before.HeapBytes) + " → " + BevToolsFmt.Bytes(heapAfter),
            Detail = (exact ? "the heap after it, as the runtime reported" : "the heap at the first reading after it")
                + (paused > 0 ? "; the runtime paused " + BevToolsFmt.Ms(paused) + " for collections in the last second" : "")
                + (session.IsInMultiUserHost ? "; process-wide: every circuit shares this heap" : ""),
            DurationMs = pause,
            Severity = pause >= 50 ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
        });
        session.Touch();
        return true;
    }

    /// <summary>
    /// The heap after full collections growing steadily: in the last ten minutes at least five of them, the last at least
    /// half again the first and 20 MB more, never back near where it started. What a leak looks like from outside; what
    /// holds the objects is a heap snapshot's question.
    /// </summary>
    internal (BevToolsFullCollection First, BevToolsFullCollection Last, int Count)? Growth(DateTimeOffset now)
    {
        var recent = _collections.ToArray().Where(c => c.At >= now - TimeSpan.FromMinutes(10)).ToArray();
        if (recent.Length < 5)
        {
            return null;
        }

        var first = recent[0];
        var last = recent[^1];
        var floor = recent.Skip(1).Min(c => c.HeapAfterBytes);
        return last.HeapAfterBytes >= first.HeapAfterBytes * 1.5 && last.HeapAfterBytes - first.HeapAfterBytes >= 20L * 1024 * 1024 && floor > first.HeapAfterBytes * 1.1
            ? (first, last, recent.Length)
            : null;
    }
}
