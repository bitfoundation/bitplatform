namespace Bit.BevTools;

/// <summary>One reading of the runtime's memory: what the managed heap holds, the collections so far, the pause time.</summary>
public sealed record BevMemorySample(DateTimeOffset At, long HeapBytes, int FullCollections, double PauseMs)
{
    /// <summary>The WebAssembly linear memory (everything the runtime has from the browser; it never shrinks); null elsewhere.</summary>
    public long? LinearMemoryBytes { get; init; }
}

/// <summary>
/// The managed heap after a full collection: what survived it, the series a leak shows up in. <see cref="Exact"/> when the
/// runtime reported the collection itself (<c>GC.GetGCMemoryInfo(GCKind.FullBlocking)</c>); otherwise the heap at the
/// first reading after it, which can include what was allocated in between.
/// </summary>
public sealed record BevFullCollection(DateTimeOffset At, long HeapAfterBytes, double? PauseMs, bool Exact);

/// <summary>What the runtime's counters say, read at once.</summary>
internal readonly record struct BevMemoryReading(long HeapBytes, int FullCollections, double PauseMs, long LastFullIndex, long LastFullHeapBytes, double? LastFullPauseMs, long? LinearMemoryBytes)
{
    /// <summary>Full collections are the highest generation's: gen 2 on CoreCLR, gen 1 on Mono (WebAssembly), whose GC has two.</summary>
    public static BevMemoryReading Now(long? linearMemory)
    {
        long index = 0, heapAfter = 0;
        double? lastPause = null;
        double pause = 0;
        try
        {
            // Every member read inside: on Mono (WebAssembly) the info is a default whose getters throw.
            var full = GC.GetGCMemoryInfo(GCKind.FullBlocking);
            index = full.Index;
            if (index > 0)
            {
                heapAfter = full.HeapSizeBytes - full.FragmentedBytes;
                lastPause = full.PauseDurations.Length > 0 ? full.PauseDurations[0].TotalMilliseconds : null;
            }
        }
        catch
        {
            // A runtime that keeps no record of its collections: the heap and the counts still tell the story.
            index = 0;
        }

        try
        {
            pause = GC.GetTotalPauseDuration().TotalMilliseconds;
        }
        catch
        {
            pause = 0;
        }

        return new BevMemoryReading(GC.GetTotalMemory(false), GC.CollectionCount(GC.MaxGeneration), pause, index, heapAfter, lastPause, linearMemory);
    }
}

/// <summary>
/// The session's memory over time, read once a second while BevTools' panel component is on the page (open or not) and
/// kept while capture is on (paused, nothing is added): the
/// heap, full collections - each a timeline event, so a slow interaction or a long frame can be read next to the pause
/// that came with it - the heap after each full collection, and on WebAssembly the linear memory. The last ten minutes.
/// On a server the heap and its collections are the process's, shared by every circuit, and the panel says so.
/// </summary>
public sealed class BevMemoryTrend
{
    private const int Capacity = 600;
    private readonly RingBuffer<BevMemorySample> _samples = new(Capacity);
    private readonly RingBuffer<BevFullCollection> _collections = new(100);
    private readonly object _lock = new();
    private BevMemoryReading? _last;
    private DateTimeOffset _lastAt;

    public BevMemorySample[] Samples => _samples.ToArray();

    /// <summary>The full collections seen while sampling, oldest first.</summary>
    public BevFullCollection[] FullCollections => _collections.ToArray();

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
    internal bool Sample(BevToolsSession session, DateTimeOffset now, Func<BevMemoryReading> read)
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
            _samples.Add(new BevMemorySample(now, reading.HeapBytes, reading.FullCollections, reading.PauseMs) { LinearMemoryBytes = reading.LinearMemoryBytes });
        }

        BevMemoryReading? previous;
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
        _collections.Add(new BevFullCollection(now, heapAfter, pause, exact));
        var paused = reading.PauseMs - before.PauseMs;
        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Custom,
            Category = "gc",
            Title = (count == 1 ? "Full garbage collection" : count + " full garbage collections") + ": heap " + BevFmt.Bytes(before.HeapBytes) + " → " + BevFmt.Bytes(heapAfter),
            Detail = (exact ? "the heap after it, as the runtime reported" : "the heap at the first reading after it")
                + (paused > 0 ? "; the runtime paused " + BevFmt.Ms(paused) + " for collections in the last second" : "")
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
    internal (BevFullCollection First, BevFullCollection Last, int Count)? Growth(DateTimeOffset now)
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
