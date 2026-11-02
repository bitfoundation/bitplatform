using BenchmarkDotNet.Attributes;

namespace Bit.Blow.Benchmarks;

/// <summary>
/// One routed edge, the way the canvas routes it: across a block of the grid workload, round every node within reach of
/// its two ends. A drag re-routes every routed edge once per frame, so this is the cost per edge per frame.
/// </summary>
[MemoryDiagnoser]
public class RoutingBenchmarks
{
    private List<BlowRect> _obstacles = null!;
    private BlowXYPosition _source;
    private BlowXYPosition _target;

    /// <summary>How many nodes stand between and around the edge's two ends.</summary>
    [Params(10, 40, 100)]
    public int Obstacles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (nodes, _) = Workloads.Grid(Obstacles);
        _obstacles = nodes.Select(n => new BlowRect(n.Position.X, n.Position.Y, n.Width!.Value, n.Height!.Value)).ToList();
        var right = _obstacles.Max(r => r.Right);
        var bottom = _obstacles.Max(r => r.Bottom);
        _source = new BlowXYPosition(-100, -60);
        _target = new BlowXYPosition(right + 100, bottom + 60);

        // A benchmark of a search that gave up would measure nothing.
        if (RouteAcross() is null) throw new InvalidOperationException("No route was found across the grid.");
    }

    [Benchmark]
    public BlowPathResult? RouteAcross()
        => BlowEdgeRouting.Route(_source, BlowPosition.Right, _target, BlowPosition.Left, _obstacles);
}
