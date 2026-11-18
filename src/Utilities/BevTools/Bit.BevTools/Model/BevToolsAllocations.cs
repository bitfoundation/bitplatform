namespace Bit.BevTools;

/// <summary>Whether this runtime counts the bytes each thread allocates, which per-render allocation needs.</summary>
public static class BevToolsAllocations
{
    /// <summary>
    /// True when <see cref="GC.GetAllocatedBytesForCurrentThread"/> moves when this thread allocates. Probed once: a runtime
    /// that always answers zero must not make every render read as allocating nothing.
    /// </summary>
    public static bool IsMeasurable { get; } = Probe();

    private static bool Probe()
    {
        try
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(new byte[1024]);
            return GC.GetAllocatedBytesForCurrentThread() - before >= 1024;
        }
        catch
        {
            return false;
        }
    }
}
