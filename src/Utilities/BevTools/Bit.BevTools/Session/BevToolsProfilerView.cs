namespace Bit.BevTools;

internal sealed class BevToolsProfilerView
{
    public BevToolsSortState Sorting { get; } = new("total", ascending: false);

    public bool ByType { get; set; }

    public bool SinceBaseline { get; set; }

    public bool IncludeDisposed { get; set; }

    /// <summary>The render batch whose cost tree is shown, and its timeline event (gone once the timeline is cleared).</summary>
    public long? BatchId { get; set; }

    public long? BatchEventId { get; set; }

    /// <summary>The selected batch as a tree (self and subtree time) or on a time axis.</summary>
    public bool BatchOnTimeAxis { get; set; }

    /// <summary>Whether the renders-per-page section is open; it reads every retained render, so only while it is.</summary>
    public bool ShowRoutes { get; set; }

    /// <summary>The uploaded export's profile, so the comparison can be run again after another interaction.</summary>
    public BevToolsRenderProfile? ComparedProfile { get; set; }

    public string? ComparedFile { get; set; }

    public BevToolsProfileComparisonResult? Comparison { get; set; }

    public string? ComparisonError { get; set; }
}
