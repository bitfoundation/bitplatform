
namespace Bit.BevTools;

/// <summary>Snapshot of a component instance made available to diagnostics rules.</summary>
public sealed record BevToolsComponentSnapshot(
    long InstanceId,
    string TypeName,
    string DisplayName,
    long? ParentInstanceId,
    int Depth,
    int RenderCount,
    double TotalRenderMs,
    double LastRenderMs,
    double MaxRenderMs,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastRenderAt,
    bool IsDisposed,
    int ErrorCount,
    bool IsFrameworkInternal = false,
    int ParentRendersWithUnchangedParameters = 0)
{
    public double AverageRenderMs => RenderCount == 0 ? 0 : TotalRenderMs / RenderCount;
}
