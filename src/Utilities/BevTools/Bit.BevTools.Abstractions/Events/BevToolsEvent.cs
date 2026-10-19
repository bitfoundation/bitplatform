namespace Bit.BevTools;

/// <summary>Broad classification of a timeline entry. Extensions use <see cref="Custom"/> with their own <see cref="BevToolsEvent.Category"/>.</summary>
public enum BevToolsEventKind
{
    Render,
    RenderBatch,
    Lifecycle,
    UiEvent,
    StateChange,
    Navigation,
    Http,
    JsInterop,
    Error,
    Circuit,
    Browser,
    Diagnostic,
    Custom,
}

public enum BevToolsSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>
/// One entry of the unified activity timeline. Instances are immutable from the point of view of extensions,
/// except for <see cref="DurationMs"/>, <see cref="Detail"/> and <see cref="Severity"/> which the runtime may
/// finalize once an asynchronous activity completes.
/// </summary>
public class BevToolsEvent
{
    /// <summary>Monotonic identifier assigned by the timeline when the event is recorded.</summary>
    public long Id { get; set; }

    /// <summary>Wall-clock time the activity started.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>High-resolution start timestamp (<see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>) for ordering and duration math.</summary>
    public long StartTicks { get; init; } = System.Diagnostics.Stopwatch.GetTimestamp();

    public BevToolsEventKind Kind { get; init; } = BevToolsEventKind.Custom;

    /// <summary>Short machine-friendly category such as "render", "http", "state:AppState".</summary>
    public string Category { get; init; } = "custom";

    public string Title { get; init; } = string.Empty;

    /// <summary>Optional longer description shown when the event is selected.</summary>
    public string? Detail { get; set; }

    public double? DurationMs { get; set; }

    public BevToolsSeverity Severity { get; set; } = BevToolsSeverity.Info;

    /// <summary>The event that caused this one, when known (e.g. a UI event that caused a render).</summary>
    public long? ParentEventId { get; set; }

    /// <summary>Stable identifier of the component instance this event relates to, when known.</summary>
    public long? ComponentInstanceId { get; init; }

    /// <summary>
    /// Component type this event belongs to. Settable because some sources only publish it when the activity ends
    /// (the framework tags its HandleEvent activity on stop, not on start).
    /// </summary>
    public string? ComponentName { get; set; }

    /// <summary>Small bag of structured data. Values are inspected lazily by the UI; keep them small.</summary>
    public IReadOnlyDictionary<string, object?>? Data { get; init; }

    /// <summary>
    /// Reads one entry of <see cref="Data"/>. Use this rather than the indexer: an event recorded while an activity
    /// is still in progress does not yet carry every key, and a rule or panel must not throw over a missing one.
    /// </summary>
    public bool TryGetData(string key, out object? value)
    {
        if (Data is not null)
        {
            return Data.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    /// <summary>Reads one entry of <see cref="Data"/> as a string, or null when it is absent.</summary>
    public string? DataString(string key) => TryGetData(key, out var value) ? value?.ToString() : null;

    public override string ToString() => $"[{Kind}] {Title}";
}
