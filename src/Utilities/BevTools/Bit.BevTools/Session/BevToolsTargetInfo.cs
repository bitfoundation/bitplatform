namespace Bit.BevTools;

/// <summary>One BevTools runtime on the page, as the page hub knows it.</summary>
public sealed class BevToolsTargetInfo
{
    public string Id { get; set; } = "";

    public long? SessionId { get; set; }

    public string? Platform { get; set; }

    public string? RenderMode { get; set; }

    public string Label { get; set; } = "";

    public double AttachedAt { get; set; }
}
