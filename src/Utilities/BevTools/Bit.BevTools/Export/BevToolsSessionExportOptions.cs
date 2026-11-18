namespace Bit.BevTools;

/// <summary>What <see cref="BevToolsSessionExporter.ToJson"/> includes. Everything is bounded by the session's own buffers.</summary>
public sealed class BevToolsSessionExportOptions
{
    /// <summary>Include per-event timeline rows (the largest section). Default true.</summary>
    public bool IncludeTimeline { get; set; } = true;

    /// <summary>Include the structured <c>Data</c> bag of each timeline event. Default true.</summary>
    public bool IncludeEventData { get; set; } = true;

    /// <summary>Include individual JS interop calls in addition to the per-identifier aggregates. Default true.</summary>
    public bool IncludeInteropCalls { get; set; } = true;

    /// <summary>Include the change history of state providers (diffs only, never full snapshots). Default true.</summary>
    public bool IncludeStateHistory { get; set; } = true;

    /// <summary>
    /// Include the render profile per component type (<c>profile</c>), what <see cref="BevToolsProfileComparison"/> compares.
    /// Default true; a frontend that polls and never shows it leaves it out, as it reads every retained render sample.
    /// </summary>
    public bool IncludeProfile { get; set; } = true;

    /// <summary>Include recently disposed components in the component list. Default true.</summary>
    public bool IncludeDisposedComponents { get; set; } = true;

    /// <summary>Pretty-print the JSON. Default true: exports are read by people first.</summary>
    public bool Indented { get; set; } = true;

    /// <summary>
    /// Only the timeline events after this id, plus the most recent ones (whose duration or detail can still change
    /// while their activity completes). For a frontend that already holds the rest; null writes the whole buffer.
    /// </summary>
    public long? TimelineAfterEventId { get; set; }

    /// <summary>Only these top-level sections ("components", "timeline", "http", …); null writes every one.</summary>
    public IReadOnlySet<string>? Sections { get; set; }

    internal bool Includes(string section) => Sections is null || Sections.Contains(section);
}
