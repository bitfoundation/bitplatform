namespace Bit.Butil;

/// <summary>
/// The four members every entry on the performance timeline carries, whatever kind it is. The typed
/// entries derive from this; <see cref="Performance.GetTypedEntries"/> returns it as it stands for
/// the kinds that add nothing - marks, measures and paint timings.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEntry">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceEntry</see>
/// </summary>
public class PerformanceEntry
{
    /// <summary>The entry's name - a URL for a resource, the mark's name for a mark, and so on.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The kind of entry: <c>"navigation"</c>, <c>"resource"</c>, <c>"mark"</c>, <c>"longtask"</c>...</summary>
    public string EntryType { get; set; } = string.Empty;

    /// <summary>When it happened, in milliseconds since the time origin.</summary>
    public double StartTime { get; set; }

    /// <summary>How long it took, in milliseconds. Zero for the entry kinds that are instants rather than intervals.</summary>
    public double Duration { get; set; }
}
