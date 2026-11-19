namespace Bit.BevTools;

/// <summary>What the runtime's counters say, read at once.</summary>
internal readonly record struct BevToolsMemoryReading(long HeapBytes, int FullCollections, double PauseMs, long LastFullIndex, long LastFullHeapBytes, double? LastFullPauseMs, long? LinearMemoryBytes)
{
    /// <summary>Full collections are the highest generation's: gen 2 on CoreCLR, gen 1 on Mono (WebAssembly), whose GC has two.</summary>
    public static BevToolsMemoryReading Now(long? linearMemory)
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

        return new BevToolsMemoryReading(GC.GetTotalMemory(false), GC.CollectionCount(GC.MaxGeneration), pause, index, heapAfter, lastPause, linearMemory);
    }
}
