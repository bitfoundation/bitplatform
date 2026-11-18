namespace Bit.BevTools;

/// <summary>When <see cref="BevToolsProfileComparison"/> calls a change a regression or an improvement.</summary>
public sealed class BevToolsProfileComparisonOptions
{
    /// <summary>A type renders this many times more (or fewer) at least. Default 1: the same interaction renders the same number of times.</summary>
    public int MinRenderChange { get; set; } = 1;

    /// <summary>
    /// …and by more than this fraction of the baseline's renders. Default 0, so counts are compared exactly; raise it
    /// (0.1 tolerates 100 → 110) for recordings made by hand, where a timer or a late response can add a render.
    /// </summary>
    public double RenderChangeRatio { get; set; }

    /// <summary>A type's total render time changes by this many milliseconds at least. Default 5.</summary>
    public double MinTimeChangeMs { get; set; } = 5;

    /// <summary>…and by more than this fraction of the baseline's time. Default 0.5: timings vary from run to run, counts do not.</summary>
    public double TimeChangeRatio { get; set; } = 0.5;

    /// <summary>
    /// A type's bytes allocated per render change by this many at least. Default 1024. Compared only when both recordings
    /// measured allocations, in the same render mode; per render, so a type that renders more often is a render regression, not also this one.
    /// </summary>
    public long MinAllocationChangeBytes { get; set; } = 1024;

    /// <summary>…and by more than this fraction of the baseline's bytes per render. Default 0.25: allocations repeat far more closely than times.</summary>
    public double AllocationChangeRatio { get; set; } = 0.25;
}
