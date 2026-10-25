namespace Bit.BevTools;

/// <summary>
/// Entry point for libraries and applications that want to add their own activity to the BevTools timeline.
/// Resolve it from DI (scoped). Recording is cheap and bounded: when the buffer is full the oldest entries are dropped.
/// </summary>
public interface IBevToolsTimeline
{
    /// <summary>Records an event and returns its id. Returns -1 when BevTools is disabled.</summary>
    long Record(BevToolsEvent bevToolsEvent);

    /// <summary>Marks a previously recorded event as completed, setting its duration and optional detail/severity.</summary>
    void Complete(long eventId, double durationMs, string? detail = null, BevToolsSeverity? severity = null);

    /// <summary>Records a custom event that started now and finishes when the returned scope is disposed.</summary>
    IDisposable BeginActivity(string category, string title, string? detail = null, BevToolsEventKind kind = BevToolsEventKind.Custom);

    /// <summary>True when BevTools is enabled for this session; use it to skip building expensive event payloads.</summary>
    bool IsEnabled { get; }
}
