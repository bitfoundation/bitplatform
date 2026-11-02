using BenchmarkDotNet.Attributes;

namespace Bit.Blow.Benchmarks;

/// <summary>
/// The helpers that run outside the render loop but scale with the graph: auto-layout, edge geometry, recording
/// an undo step and copy/paste. Each runs once per user action, so the budget is "imperceptible" (well under
/// 100 ms) rather than a frame.
/// </summary>
[MemoryDiagnoser]
public class GraphBenchmarks
{
    private List<BlowNode> _nodes = null!;
    private List<BlowEdge> _edges = null!;
    private BlowHistory _history = null!;
    private BlowFragment _half = null!;
    private int _step;

    /// <summary>Nodes in the graph; edges follow at about 1.3 per node.</summary>
    [Params(1000, 5000)]
    public int Nodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_nodes, _edges) = Workloads.Grid(Nodes);
        _history = new BlowHistory(100);
        _history.Capture(Snapshot());
        _half = BlowClipboard.Copy(_nodes.Take(Nodes / 2), _edges);
    }

    private BlowSnapshot Snapshot()
        => new(_nodes.Select(n => n.Clone()).ToList(), _edges.Select(e => e.Clone()).ToList(), BlowViewport.Identity);

    /// <summary>Layered (Sugiyama-style) auto-layout of the whole graph.</summary>
    [Benchmark]
    public void LayeredLayout() => BlowLayout.Layered(_nodes, _edges);

    /// <summary>Building the SVG path of every edge, as a first render or a full relayout does.</summary>
    [Benchmark]
    public int EdgePaths()
    {
        var length = 0;
        foreach (var e in _edges)
        {
            var result = BlowEdgePath.Compute(e.Type, 0, 0, BlowPosition.Right, 220, 120, BlowPosition.Left);
            length += result.Path.Length;
        }
        return length;
    }

    /// <summary>
    /// Recording one undo step after a single node moved, the way the canvas's built-in history does at the end of
    /// a drag: each live element is compared with its copy in the previous step, and only the one that changed is
    /// copied again.
    /// </summary>
    [Benchmark]
    public bool CaptureUndoStep()
    {
        _nodes[0].Position = new BlowXYPosition(++_step, 0);
        return _history.Capture(_nodes, _edges, BlowViewport.Identity, "move");
    }

    /// <summary>Pasting half the graph back in: fresh ids, rewired edges, offset positions.</summary>
    [Benchmark]
    public int PasteHalf()
        => BlowClipboard.Paste(_half, new BlowPasteOptions { ExistingIds = _nodes.Select(n => n.Id) }).Nodes.Count;
}
