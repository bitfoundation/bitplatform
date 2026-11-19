namespace Bit.BevTools;

/// <summary>One component type in both recordings. Times are totals of BuildRenderTree in milliseconds.</summary>
public sealed record BevToolsProfileTypeChange(
    string Type,
    string DisplayName,
    BevToolsProfileChangeKind Kind,
    int BaselineInstances,
    int CurrentInstances,
    int BaselineRenders,
    int CurrentRenders,
    double BaselineRenderMs,
    double CurrentRenderMs,
    double BaselineMaxRenderMs,
    double CurrentMaxRenderMs,
    double? BaselineP95RenderMs,
    double? CurrentP95RenderMs,
    string? Reason)
{
    /// <summary>Bytes the baseline's renders of the type allocated; null when that recording did not measure allocations.</summary>
    public long? BaselineAllocatedBytes { get; init; }

    public long? CurrentAllocatedBytes { get; init; }

    /// <summary>Bytes allocated per render in the baseline, when measured.</summary>
    public double? BaselineBytesPerRender => BaselineAllocatedBytes is { } bytes && BaselineRenders > 0 ? (double)bytes / BaselineRenders : null;

    public double? CurrentBytesPerRender => CurrentAllocatedBytes is { } bytes && CurrentRenders > 0 ? (double)bytes / CurrentRenders : null;
}
