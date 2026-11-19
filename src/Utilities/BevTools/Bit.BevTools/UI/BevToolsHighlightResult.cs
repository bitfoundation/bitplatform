namespace Bit.BevTools;

/// <summary>What the page hub says after outlining a component.</summary>
internal sealed class BevToolsHighlightResult
{
    public bool Ok { get; set; }

    /// <summary>False when the component renders nothing with a size: no element, or only empty ones.</summary>
    public bool Visible { get; set; }

    public string? Error { get; set; }
}
