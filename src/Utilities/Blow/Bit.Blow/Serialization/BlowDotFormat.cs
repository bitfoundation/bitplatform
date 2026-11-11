using System.Globalization;
using System.Text;

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

/// <summary>
/// Graphviz DOT out: nodes, edges, labels, ports and groups (as clusters), for Graphviz's layouts and renderers and the
/// many tools that read DOT. Every id and label is written as a quoted, escaped string, so no input can break out of
/// it.
/// </summary>
public static class BlowDotFormat
{
    /// <summary>Writes the document as a directed DOT graph.</summary>
    public static string Export(BlowDocument document, BlowDotExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var o = options ?? new BlowDotExportOptions();
        var nodes = document.Nodes.Where(n => o.IncludeHidden || !n.Hidden).GroupBy(n => n.Id).Select(g => g.First()).ToList();
        var present = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        var horizontal = nodes.Count(n => n.SourcePosition == BlowPosition.Right) * 2 > nodes.Count;

        // Groups become clusters, which DOT cannot point an edge at: an edge to a group goes to a node inside it, clipped
        // at the cluster's border (compound=true with lhead/ltail).
        var clusters = new Dictionary<string, (string Name, string Inner)>(StringComparer.Ordinal);
        var body = new StringBuilder();
        var open = new Stack<(string Id, string Name)>();
        var cluster = 0;
        foreach (var (node, depth, step) in BlowDiagramText.Walk(nodes))
        {
            var indent = BlowDiagramText.Indent(depth);
            switch (step)
            {
                case BlowDiagramText.Step.Enter:
                    var name = $"cluster_{cluster++}";
                    open.Push((node.Id, name));
                    body.Append(indent).Append("subgraph ").Append(Quote(name)).Append(" {\n");
                    body.Append(indent).Append("    label=").Append(Quote(Name(node))).Append(";\n");
                    break;
                case BlowDiagramText.Step.Exit:
                    open.Pop();
                    body.Append(indent).Append("}\n");
                    break;
                default:
                    // The first leaf inside each open cluster stands for it.
                    foreach (var (id, clusterName) in open) clusters.TryAdd(id, (clusterName, node.Id));
                    body.Append(indent).Append(Quote(node.Id)).Append(" [label=").Append(Quote(Name(node)));
                    if (o.IncludePositions)
                    {
                        var p = node.AbsolutePosition;
                        var size = node.EffectiveSize;
                        // Graphviz places a node by its centre, in points, with y growing upwards.
                        body.Append(", pos=").Append(Quote(string.Create(CultureInfo.InvariantCulture,
                            $"{(p.X + size.Width / 2) * 0.75:0.##},{-(p.Y + size.Height / 2) * 0.75:0.##}!")));
                    }
                    body.Append("];\n");
                    break;
            }
        }

        var text = new StringBuilder();
        text.Append("digraph ").Append(Quote(o.Name)).Append(" {\n");
        if (horizontal) text.Append("    rankdir=LR;\n");
        if (clusters.Count > 0) text.Append("    compound=true;\n");
        text.Append("    node [shape=box, style=rounded];\n");
        text.Append(body);

        foreach (var edge in document.Edges)
        {
            if ((!o.IncludeHidden && edge.Hidden) || !present.Contains(edge.Source) || !present.Contains(edge.Target)) continue;
            var from = clusters.GetValueOrDefault(edge.Source);
            var to = clusters.GetValueOrDefault(edge.Target);
            text.Append("    ").Append(Quote(from.Inner ?? edge.Source));
            if (from.Inner is null && !string.IsNullOrEmpty(edge.SourceHandle)) text.Append(':').Append(Quote(edge.SourceHandle));
            text.Append(" -> ").Append(Quote(to.Inner ?? edge.Target));
            if (to.Inner is null && !string.IsNullOrEmpty(edge.TargetHandle)) text.Append(':').Append(Quote(edge.TargetHandle));

            var attributes = new List<string>();
            if (from.Name is not null) attributes.Add("ltail=" + Quote(from.Name));
            if (to.Name is not null) attributes.Add("lhead=" + Quote(to.Name));
            if (!string.IsNullOrWhiteSpace(edge.Label)) attributes.Add("label=" + Quote(edge.Label!));
            if (edge.Animated) attributes.Add("style=dashed");
            if (!edge.MarkerEnd && !edge.MarkerStart) attributes.Add("arrowhead=none");
            else if (edge.MarkerStart && edge.MarkerEnd) attributes.Add("dir=both");
            else if (edge.MarkerStart) attributes.Add("dir=back");
            if (attributes.Count > 0) text.Append(" [").Append(string.Join(", ", attributes)).Append(']');
            text.Append(";\n");
        }
        return text.Append("}\n").ToString();
    }

    private static string Name(BlowNode node) => string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label!;

    /// <summary>A DOT quoted string: backslashes and quotes escaped, line breaks as DOT's <c>\n</c>, other control characters dropped.</summary>
    internal static string Quote(string value)
    {
        var b = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in BlowDiagramText.XmlSafe(value))
        {
            switch (c)
            {
                case '"': b.Append("\\\""); break;
                case '\\': b.Append("\\\\"); break;
                case '\n': b.Append("\\n"); break;
                case '\r': break;
                default:
                    if (!char.IsControl(c)) b.Append(c);
                    break;
            }
        }
        return b.Append('"').ToString();
    }
}
