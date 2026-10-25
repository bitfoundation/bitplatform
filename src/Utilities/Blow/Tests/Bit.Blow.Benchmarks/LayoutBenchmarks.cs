using BenchmarkDotNet.Attributes;

namespace Bit.Blow.Benchmarks;

/// <summary>
/// The tree, radial and force layouts, at the sizes the other graph helpers are measured at. Tree and radial are
/// linear; force pushes every pair apart exactly up to <see cref="BlowLayout.ExactRepulsionLimit"/> nodes and with
/// Barnes-Hut past it, so an iteration costs n log n.
/// </summary>
[MemoryDiagnoser]
public class LayoutBenchmarks
{
    private List<BlowNode> _nodes = null!;
    private List<BlowEdge> _edges = null!;

    /// <summary>Nodes in the graph.</summary>
    [Params(200, 1000, 5000)]
    public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup() => (_nodes, _edges) = Workloads.Grid(Nodes);

    /// <summary>The breadth-first spanning tree of the grid, laid out top to bottom.</summary>
    [Benchmark]
    public void TreeLayout() => BlowLayout.Tree(_nodes, _edges);

    /// <summary>The same tree in rings.</summary>
    [Benchmark]
    public void RadialLayout() => BlowLayout.Radial(_nodes, _edges);

    /// <summary>The default 300 iterations of the force simulation, then overlap removal.</summary>
    [Benchmark]
    public void ForceLayout() => BlowLayout.Force(_nodes, _edges);

    /// <summary>The same with every pair pushed apart exactly (θ = 0), as every size was before Barnes-Hut. Skipped at 5,000 nodes.</summary>
    [Benchmark]
    public void ForceLayoutExact()
    {
        if (Nodes > 1000) return;
        BlowLayout.Force(_nodes, _edges, new BlowForceLayoutOptions { Theta = 0 });
    }
}
