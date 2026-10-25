using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Benchmarks;

/// <summary>
/// The C# side of a canvas render: building the render tree for every node, edge and handle and diffing it. This
/// is the per-frame cost of a pan, a zoom or a drag on Blazor WebAssembly, and on Blazor Server it runs on the
/// server for every connected user. The browser's layout and paint are not included - they are measured by the
/// E2E stress scenario instead.
/// </summary>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    private HeadlessRenderer _renderer = null!;
    private BlowCanvas _canvas = null!;
    private List<BlowNode> _nodes = null!;
    private int _frame;

    /// <summary>Nodes in the graph; edges follow at about 1.3 per node.</summary>
    [Params(200, 1000)]
    public int Nodes { get; set; }

    [GlobalSetup(Targets = [nameof(PanFrame), nameof(ZoomFrame)])]
    public void Setup()
    {
        (_nodes, var edges) = Workloads.Grid(Nodes);
        _renderer = new HeadlessRenderer();
        _canvas = _renderer.RenderAsync<BlowCanvas>(new()
        {
            [nameof(BlowCanvas.Nodes)] = _nodes,
            [nameof(BlowCanvas.Edges)] = edges,
        }).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Not timed: how many components the last measured frame rendered, which is what the time is made of.
        Console.WriteLine($"// {Nodes} nodes: the last frame rendered {_renderer.LastBatchComponents} components");
        _renderer.Dispose();
    }

    private void OnDispatcher(Action action) => _renderer.Dispatcher.InvokeAsync(action).GetAwaiter().GetResult();

    /// <summary>One frame of a pan: the viewport moves, nothing in the graph does.</summary>
    [Benchmark]
    public void PanFrame() => OnDispatcher(() => _canvas.SetViewport(new BlowViewport(++_frame % 100, 0, 1)));

    /// <summary>One frame of a zoom about the pane centre.</summary>
    [Benchmark]
    public void ZoomFrame() => OnDispatcher(() => _canvas.ZoomTo(1 + ++_frame % 10 / 20.0));

    /// <summary>
    /// One frame of dragging a single node: a pointer move while a drag is running, the way the browser delivers
    /// it. One node and its two or three edges move.
    /// </summary>
    [Benchmark]
    public void DragFrame()
    {
        var x = ++_frame % 100 + 10;
        OnDispatcher(() => _pointerMove.Invoke(_canvas, [new PointerEventArgs { PointerId = 1, ClientX = x, ClientY = 5 }]));
    }

    /// <summary>The drag benchmark's graph, with a drag of the first node already under way.</summary>
    [GlobalSetup(Target = nameof(DragFrame))]
    public void SetupDrag()
    {
        Setup();
        OnDispatcher(() => ((IBlowContext)_canvas).BeginNodeDrag(_nodes[0], new PointerEventArgs { PointerId = 1, ClientX = 5, ClientY = 5 }));
    }

    /// <summary>
    /// The canvas's pointer-move handler. It is private - in a browser it is reached through the DOM event - so the
    /// benchmark calls it directly, which is exactly what Blazor's event dispatch ends up doing.
    /// </summary>
    private readonly System.Reflection.MethodInfo _pointerMove = typeof(BlowCanvas).GetMethod(
        "OnPanePointerMove", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
}
