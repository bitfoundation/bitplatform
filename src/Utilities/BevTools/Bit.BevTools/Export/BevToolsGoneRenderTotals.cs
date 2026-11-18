namespace Bit.BevTools;

/// <summary>Render totals of one component type's instances that the registry let go after their retention.</summary>
internal sealed record BevToolsGoneRenderTotals
{
    public required string TypeName { get; init; }

    public required string DisplayName { get; init; }

    public int Instances { get; set; }

    public int Renders { get; set; }

    public double RenderMs { get; set; }

    public double MaxRenderMs { get; set; }

    public int InstancesSinceBaseline { get; set; }

    public int RendersSinceBaseline { get; set; }

    public double RenderMsSinceBaseline { get; set; }

    public double MaxRenderMsSinceBaseline { get; set; }

    public long AllocatedBytes { get; set; }

    public long AllocatedBytesSinceBaseline { get; set; }
}
