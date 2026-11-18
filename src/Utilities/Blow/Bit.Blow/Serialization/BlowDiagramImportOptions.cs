namespace Bit.Blow;

/// <summary>Settings for importing a diagram format that has no positions of its own, or not always (Mermaid, GraphML).</summary>
public sealed class BlowDiagramImportOptions
{
    /// <summary>The bounds the input must stay within, as for Blow documents. <see cref="BlowImportLimits.MaxDepth"/> bounds how deep groups nest.</summary>
    public BlowImportLimits Limits { get; set; } = new();

    /// <summary>
    /// Arranges the nodes when the input gave no positions: each group's children inside it, then the top level. A
    /// layered layout by default, in the direction the input asked for; null leaves every node at the origin.
    /// </summary>
    public IBlowLayout? Layout { get; set; } = new BlowLayeredLayoutOptions();

    /// <summary>Space left between a group's edge and its children when groups are sized round them.</summary>
    public double GroupPadding { get; set; } = 24;

    /// <summary>The most issues an import reports; past it one last issue says the rest were left out. 1,000 by default.</summary>
    public int MaxIssues { get; set; } = 1000;
}
