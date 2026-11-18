namespace Bit.BevTools;

/// <summary>What one component type rendered in a profile's window. <see cref="Type"/> is <see cref="BevToolsRenderProfile.TypeKey"/>.</summary>
internal sealed record BevToolsProfileTypeTotals(string Type, string DisplayName, int Instances, int Renders, double RenderMs, double MaxRenderMs, double? P95RenderMs, int RetainedSamples)
{
    /// <summary>Bytes its renders allocated in the window; null when the recording's runtime did not measure them.</summary>
    public long? AllocatedBytes { get; init; }
}
