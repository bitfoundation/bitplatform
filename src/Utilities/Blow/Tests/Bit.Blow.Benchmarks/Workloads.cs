namespace Bit.Blow.Benchmarks;

/// <summary>
/// Synthetic graphs shaped like the ones flow editors actually hold: a pipeline-like DAG where most nodes feed one
/// or two later ones, laid out on a grid so a viewport sees a realistic fraction of it, with real sizes so nothing
/// falls back to the unmeasured default.
/// </summary>
internal static class Workloads
{
    /// <summary>
    /// A grid of <paramref name="count"/> nodes, each wired to its right-hand neighbour and, every third node, to
    /// the node below it - roughly 1.3 edges per node, the density of a typical workflow or data pipeline.
    /// </summary>
    public static (List<BlowNode> Nodes, List<BlowEdge> Edges) Grid(int count)
    {
        var columns = (int)Math.Ceiling(Math.Sqrt(count));
        var nodes = new List<BlowNode>(count);
        var edges = new List<BlowEdge>(count * 2);

        for (var i = 0; i < count; i++)
        {
            nodes.Add(new BlowNode
            {
                Id = $"n{i}",
                Label = $"Step {i}",
                Position = new BlowXYPosition(i % columns * 220, i / columns * 120),
                Width = 150,
                Height = 40,
                SourcePosition = BlowPosition.Right,
                TargetPosition = BlowPosition.Left,
            });
        }

        for (var i = 0; i < count; i++)
        {
            if (i % columns != columns - 1 && i + 1 < count)
                edges.Add(new BlowEdge { Id = $"h{i}", Source = $"n{i}", Target = $"n{i + 1}" });
            if (i % 3 == 0 && i + columns < count)
                edges.Add(new BlowEdge { Id = $"v{i}", Source = $"n{i}", Target = $"n{i + columns}", Type = BlowEdgeType.SmoothStep });
        }

        return (nodes, edges);
    }
}
