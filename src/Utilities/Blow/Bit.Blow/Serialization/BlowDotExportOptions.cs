namespace Bit.Blow;

/// <summary>Settings for writing Graphviz DOT.</summary>
public sealed class BlowDotExportOptions
{
    /// <summary>The graph's name. "G" by default.</summary>
    public string Name { get; set; } = "G";

    /// <summary>
    /// When true, every node is pinned where it is on the canvas (<c>pos="x,y!"</c>, in points, y pointing up as
    /// Graphviz wants), for <c>neato -n</c> or <c>fdp</c>. When false, Graphviz lays the graph out itself.
    /// </summary>
    public bool IncludePositions { get; set; }

    /// <summary>Hidden nodes and edges are left out unless this is true.</summary>
    public bool IncludeHidden { get; set; }
}
