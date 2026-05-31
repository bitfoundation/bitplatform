namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/Performance/memory">Performance.memory</see>.
/// Chrome-only, hence the explicit nulls when not available.
/// </summary>
public class PerformanceMemory
{
    public long? JsHeapSizeLimit { get; set; }
    public long? TotalJsHeapSize { get; set; }
    public long? UsedJsHeapSize { get; set; }
}
