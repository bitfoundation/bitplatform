namespace Bit.BevTools;

/// <summary>
/// The managed heap after a full collection: what survived it, the series a leak shows up in. <see cref="Exact"/> when the
/// runtime reported the collection itself (<c>GC.GetGCMemoryInfo(GCKind.FullBlocking)</c>); otherwise the heap at the
/// first reading after it, which can include what was allocated in between.
/// </summary>
public sealed record BevToolsFullCollection(DateTimeOffset At, long HeapAfterBytes, double? PauseMs, bool Exact);
