namespace Bit.Blow;

/// <summary>Settings for writing a Mermaid flowchart.</summary>
public sealed class BlowMermaidExportOptions
{
    /// <summary>The flowchart direction: <c>TB</c>, <c>LR</c>, <c>BT</c> or <c>RL</c>. Null takes it from the nodes' handle sides.</summary>
    public string? Direction { get; set; }

    /// <summary>
    /// The shape each node is drawn with, as Mermaid's brackets: <c>"[]"</c> (a box, the default), <c>"()"</c>, <c>"([])"</c>,
    /// <c>"[[]]"</c>, <c>"[()]"</c>, <c>"(())"</c>, <c>"{}"</c>, <c>"{{}}"</c>. Null draws input nodes as stadiums and
    /// the rest as boxes.
    /// </summary>
    public Func<BlowNode, string?>? Shape { get; set; }

    /// <summary>Hidden nodes and edges are left out unless this is true.</summary>
    public bool IncludeHidden { get; set; }
}
