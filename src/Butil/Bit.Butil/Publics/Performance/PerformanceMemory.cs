namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/memory">Performance.memory</see>.
/// Chrome-only, hence the explicit nulls when not available.
/// </summary>
public class PerformanceMemory
{
    /// <summary>The heap size, in bytes, the engine will not grow past.</summary>
    public long? JsHeapSizeLimit { get; set; }

    /// <summary>The currently allocated heap, in bytes.</summary>
    public long? TotalJsHeapSize { get; set; }
    
    /// <summary>The part of the allocated heap actually in use, in bytes.</summary>
    public long? UsedJsHeapSize { get; set; }
}
