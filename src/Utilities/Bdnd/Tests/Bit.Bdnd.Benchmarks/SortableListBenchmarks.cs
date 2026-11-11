using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bdnd.Benchmarks;

/// <summary>
/// The .NET side of dragging in a sortable list of <see cref="Rows"/> rows - each a drop target with edges around a
/// draggable, as the README's drop-indicator recipe builds it. What the browser module reports during a drag arrives
/// as the JS-invoked calls measured here: the start and the end of a drag (every row collects its state again, and
/// those whose state changed re-render), the edge of the hovered row changing, the hover moving from one row to the
/// next, and a key of a keyboard drag (the targets the item can go to collected and sent, and the announcement).
/// On Blazor Server each runs on the server for every connected user. Each run prints how many components the last
/// operation rendered, which is what the time is made of.
/// </summary>
[MemoryDiagnoser]
public class SortableListBenchmarks
{
    private HeadlessRenderer _renderer = null!;
    private BdndDraggable<int>[] _sources = null!;
    private BdndDropTarget<int>[] _targets = null!;
    private int _step;

    private static readonly BdndDomRect Rect = new() { Left = 10, Top = 10, Width = 200, Height = 30 };

    /// <summary>Rows of the list: every one a draggable and a drop target.</summary>
    [Params(100, 1000)]
    public int Rows { get; set; }

    [GlobalSetup(Target = nameof(DragStartAndEnd))]
    public void Setup()
    {
        _sources = new BdndDraggable<int>[Rows];
        _targets = new BdndDropTarget<int>[Rows];
        _renderer = new HeadlessRenderer();
        _renderer.Render<BdndProvider>(new()
        {
            [nameof(BdndProvider.ChildContent)] = (RenderFragment)BuildList,
        });
    }

    [GlobalSetup(Targets = [nameof(EdgeChange), nameof(HoverMove)])]
    public void SetupPointerDrag()
    {
        Setup();
        _renderer.Run(() => _sources[0].HandlePointerDragStart(20, 20, Rect));
        _renderer.Run(() => _targets[1].HandleHoverChanged(true, null));
    }

    [GlobalSetup(Target = nameof(KeyboardMove))]
    public void SetupKeyboardDrag()
    {
        Setup();
        _renderer.Run(() => _sources[0].HandleKeyboardDragStart(Rect, "Row 0"));
    }

    [IterationSetup]
    public void ResetCount() => _renderer.ResetCount();

    [GlobalCleanup]
    public void Cleanup()
    {
        // Not timed: how many components the measured operations of the last iteration rendered, per operation.
        Console.WriteLine($"// {Rows} rows: {_renderer.RenderedComponents} components rendered in the last iteration");
        _renderer.Dispose();
    }

    private void BuildList(RenderTreeBuilder builder)
    {
        for (var i = 0; i < Rows; i++)
        {
            var row = i;
            builder.OpenComponent<BdndDropTarget<int>>(0);
            builder.SetKey(row);
            builder.AddComponentParameter(1, nameof(BdndDropTarget<int>.Accept), "row");
            builder.AddComponentParameter(2, nameof(BdndDropTarget<int>.Edges), BdndEdges.Vertical);
            builder.AddComponentParameter(3, nameof(BdndDropTarget<int>.Label), $"Row {row}");
            builder.AddComponentParameter(4, nameof(BdndDropTarget<int>.ChildContent), (RenderFragment<BdndDropState<int>>)(state => b =>
            {
                b.OpenComponent<BdndDraggable<int>>(0);
                b.AddComponentParameter(1, nameof(BdndDraggable<int>.Type), "row");
                b.AddComponentParameter(2, nameof(BdndDraggable<int>.Item), row);
                b.AddComponentParameter(3, nameof(BdndDraggable<int>.Label), $"Row {row}");
                b.AddComponentParameter(4, nameof(BdndDraggable<int>.ChildContent), (RenderFragment<BdndDragState>)(drag => c =>
                {
                    c.OpenElement(0, "span");
                    c.AddAttribute(1, "class", drag.IsDragging ? "card dragging" : "card");
                    c.AddContent(2, $"Row {row}");
                    c.CloseElement();
                }));
                b.AddComponentReferenceCapture(5, source => _sources[row] = (BdndDraggable<int>)source);
                b.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(5, target => _targets[row] = (BdndDropTarget<int>)target);
            builder.CloseComponent();
        }
    }

    /// <summary>A pointer drag of the first row starting and ending with nothing dropped: the whole list collects its state twice.</summary>
    [Benchmark]
    public void DragStartAndEnd()
    {
        _renderer.Run(() => _sources[0].HandlePointerDragStart(20, 20, Rect));
        _renderer.Run(() => _sources[0].HandleDragEnd());
    }

    /// <summary>The pointer crossing the middle of the hovered row: its edge goes from top to bottom or back.</summary>
    [Benchmark]
    public void EdgeChange() => _renderer.Run(() =>
    {
        _targets[1].HandleEdgeChanged(++_step % 2 == 0 ? "top" : "bottom");
        return Task.CompletedTask;
    });

    /// <summary>The pointer moving on to the next row: one row left, another entered.</summary>
    [Benchmark]
    public void HoverMove()
    {
        var from = 1 + _step % (Rows - 2);
        var to = from + 1;
        _step++;
        _renderer.Run(async () =>
        {
            await _targets[from].HandleHoverChanged(false, null);
            await _targets[to].HandleHoverChanged(true, null);
            // round the list again from the second row
            if (to == Rows - 1)
            {
                await _targets[to].HandleHoverChanged(false, null);
                await _targets[1].HandleHoverChanged(true, null);
            }
        });
    }

    /// <summary>
    /// One arrow key of a keyboard drag: the targets the item can go to are collected and sent to the module (which
    /// answers nowhere here), and the result is announced.
    /// </summary>
    [Benchmark]
    public void KeyboardMove() => _renderer.Run(() => _sources[0].HandleKeyboardCommand("next"));
}
