namespace Bit.BevTools;

/// <summary>One reading of the runtime's memory: what the managed heap holds, the collections so far, the pause time.</summary>
public sealed record BevToolsMemorySample(DateTimeOffset At, long HeapBytes, int FullCollections, double PauseMs)
{
    /// <summary>The WebAssembly linear memory (everything the runtime has from the browser; it never shrinks); null elsewhere.</summary>
    public long? LinearMemoryBytes { get; init; }
}
