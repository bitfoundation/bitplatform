using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Bit.Bink.Benchmarks;

/// <summary>
/// The input path: what happens between a pointer event arriving and a point being stored. This runs on every
/// animation frame while drawing, so it must not allocate.
/// </summary>
[MemoryDiagnoser]
public class InputBenchmarks
{
    private BinkInputSample[] _frame = null!;
    private readonly BinkStrokeBuilder _builder = new();
    private readonly BinkBrush _brush = new BinkPenBrush { Width = 4f };

    /// <summary>Samples in one animation frame. 4 is a mouse at 60 Hz; 16 is a 240 Hz pen with coalescing.</summary>
    [Params(4, 16, 64)]
    public int SamplesPerFrame { get; set; }

    [GlobalSetup]
    public void Setup() => _frame = Workloads.Frame(SamplesPerFrame);

    /// <summary>Processing one frame of pointer samples into stored points.</summary>
    [Benchmark]
    public int ProcessFrame()
    {
        _builder.Begin(_frame[0], _brush, Matrix3x2.Identity);
        for (int i = 1; i < _frame.Length; i++) _builder.Add(_frame[i]);
        int count = _builder.Count;
        _builder.Cancel();
        return count;
    }

    /// <summary>Decoding the packed samples that arrive from the browser.</summary>
    [Benchmark]
    public float DecodeSamples()
    {
        Span<double> packed = stackalloc double[BinkInputSample.PackedSize];
        float sum = 0;
        foreach (BinkInputSample sample in _frame)
        {
            sample.WriteTo(packed);
            sum += BinkInputSample.ReadFrom(packed).X;
        }
        return sum;
    }
}

/// <summary>
/// Turning stored points into renderable geometry. This is the most expensive step per stroke, and it is cached,
/// so it happens once per stroke rather than once per frame.
/// </summary>
[MemoryDiagnoser]
public class StrokeGenerationBenchmarks
{
    private BinkStrokePoints _handwriting = null!;
    private BinkStrokePoints _long = null!;
    private readonly BinkPathBuilder _builder = new(4096);
    private readonly BinkBrush _pen = new BinkPenBrush { Width = 4f };
    private readonly BinkBrush _marker = new BinkMarkerBrush { Width = 8f };

    [GlobalSetup]
    public void Setup()
    {
        _handwriting = Workloads.HandwritingStroke(new Random(1));
        _long = Workloads.LongStroke(PointsInLongStroke);
    }

    /// <summary>Points in the long-stroke case: a continuous scribble that never lifts.</summary>
    [Params(1_000, 10_000)]
    public int PointsInLongStroke { get; set; }

    /// <summary>A typical handwriting stroke (60 points) as a variable-width outline.</summary>
    [Benchmark]
    public int VariableWidthOutline() => _pen.BuildGeometry(_handwriting.AsView(), _builder).Path.VerbCount;

    /// <summary>A constant-width centerline stroke, which produces far less geometry.</summary>
    [Benchmark]
    public int Centerline() => _marker.BuildGeometry(_handwriting.AsView(), _builder).Path.VerbCount;

    /// <summary>A very long stroke, the worst case for outline generation.</summary>
    [Benchmark]
    public int LongStrokeOutline() => _pen.BuildGeometry(_long.AsView(), _builder).Path.VerbCount;

    /// <summary>Simplification, which runs once when a stroke is committed.</summary>
    [Benchmark]
    public int Simplify() => _long.Simplify(0.1f).Count;
}

/// <summary>Recording a redraw: this runs whenever the document or the viewport changes.</summary>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    private BinkDocument _document = null!;
    private BinkCanvasRenderContext _context = null!;
    private BinkRect _visible;

    /// <summary>Strokes in the document.</summary>
    [Params(500, 5_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = Workloads.Document(Strokes);
        _context = new BinkCanvasRenderContext(1 << 20);
        _visible = new BinkRect(0, 0, 900, 600);

        // Warm the geometry cache: the first render of a document pays for geometry, later ones do not.
        foreach (BinkElement element in _document.AllElements)
        {
            if (element is BinkStroke stroke) _ = stroke.Geometry;
        }
    }

    /// <summary>Recording every element, which is what a full-page export does.</summary>
    [Benchmark]
    public int RecordEverything()
    {
        _context.Reset();
        BinkDocumentRenderer.Render(_document, _context);
        return _context.Count;
    }

    /// <summary>Recording only what is on screen, which is what an interactive redraw does.</summary>
    [Benchmark(Baseline = true)]
    public int RecordVisibleRegion()
    {
        _context.Reset();
        BinkDocumentRenderer.Render(_document, _context, new BinkRenderOptions { ClipBounds = _visible });
        return _context.Count;
    }
}

/// <summary>
/// Recording a redraw of the document surface the way the canvas does: with the browser already holding every
/// visible path, which is the case for every redraw after the first.
/// </summary>
[MemoryDiagnoser]
public class RetainedRenderBenchmarks
{
    private BinkDocument _document = null!;
    private BinkCanvasRenderContext _context = null!;
    private BinkRenderOptions _options = null!;

    /// <summary>Strokes in the document.</summary>
    [Params(500, 5_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = Workloads.Document(Strokes);
        _context = new BinkCanvasRenderContext(1 << 20, new BinkCanvasResourceCache());
        _options = new BinkRenderOptions { ClipBounds = new BinkRect(0, 0, 1600, 1000) };
        // The first frame defines every path; the benchmarks measure the ones after it, so the buffer is left holding
        // a retained frame for PackForTransport too.
        RecordRetained();
        RecordRetained();
    }

    /// <summary>A whole-page redraw that refers to retained paths.</summary>
    [Benchmark]
    public int RecordRetained()
    {
        _context.Reset();
        BinkDocumentRenderer.Render(_document, _context, _options);
        _context.EndFrame();
        return _context.Count;
    }

    /// <summary>The same frame packed as 32-bit floats, which is what crosses a Blazor Server circuit.</summary>
    [Benchmark]
    public int PackForTransport() => _context.ToSingleBytes().Length;
}

/// <summary>Editing a large selection: what dragging it costs per frame, and what undoing the drag costs.</summary>
[MemoryDiagnoser]
public class EditingBenchmarks : IDisposable
{
    private BinkDocument _document = null!;
    private BinkSelection _selection = null!;
    private BinkHistoryManager _history = null!;
    private Dictionary<Guid, System.Numerics.Matrix3x2> _start = null!;
    private int _frame;

    /// <summary>Strokes in the document, all of them selected.</summary>
    [Params(1_000, 10_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = Workloads.Document(Strokes);
        _selection = new BinkSelection(_document);
        _selection.SelectAll();
        _start = _document.AllElements.ToDictionary(e => e.Id, e => e.Transform);
        _history = new BinkHistoryManager(_document, limit: 10_000);
        // A one-second drag at 60 frames a second, recorded the way the selection tool records it.
        using (_history.BeginGroup("Move"))
        {
            for (int i = 1; i <= 60; i++) DragFrame();
        }
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <summary>Releases the history manager and selection.</summary>
    public void Dispose()
    {
        _history?.Dispose();
        _selection?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>One frame of dragging every element: a transform per element, replaced in one batch.</summary>
    [Benchmark]
    public int DragFrame()
    {
        var m = System.Numerics.Matrix3x2.CreateTranslation(++_frame % 100, 0);
        var moved = new List<BinkElement>(_selection.Count);
        foreach (BinkElement e in _selection.Elements) moved.Add(e with { Transform = _start[e.Id] * m });
        _document.ReplaceElements(moved, "Move");
        return _selection.Bounds.IsEmpty ? 0 : moved.Count;
    }

    /// <summary>Undoing and redoing the one-second drag of every element.</summary>
    [Benchmark]
    public bool UndoRedoDrag() => _history.Undo() && _history.Redo();
}

/// <summary>Hit testing and rectangle selection over a full document.</summary>
[MemoryDiagnoser]
public class QueryBenchmarks
{
    private BinkDocument _document = null!;

    /// <summary>Strokes in the document.</summary>
    [Params(1_000, 10_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup() => _document = Workloads.Document(Strokes);

    /// <summary>One click with the selection tool.</summary>
    [Benchmark]
    public bool HitTest() => BinkDocumentQuery.HitTest(_document, new Vector2(760f, 480f), 4f) is not null;

    /// <summary>A marquee selection over a quarter of the page.</summary>
    [Benchmark]
    public int MarqueeSelect() => BinkDocumentQuery.FindInRect(_document, new BinkRect(200, 200, 600, 400)).Count;
}

/// <summary>Saving, loading and exporting.</summary>
[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private BinkDocument _document = null!;
    private byte[] _saved = null!;
    private BinkStrokePoints _points = null!;
    private byte[] _encodedPoints = null!;

    /// <summary>Strokes in the document.</summary>
    [Params(1_000, 10_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = Workloads.Document(Strokes);
        _saved = BinkSerializer.SerializeToUtf8Bytes(_document);
        _points = Workloads.LongStroke(10_000);
        _encodedPoints = BinkPointCodec.Encode(_points);
    }

    /// <summary>Writing the whole document.</summary>
    [Benchmark]
    public int Save() => BinkSerializer.SerializeToUtf8Bytes(_document).Length;

    /// <summary>Reading the whole document back.</summary>
    [Benchmark]
    public int Load() => BinkSerializer.Deserialize(_saved).ElementCount;

    /// <summary>Encoding 10,000 points, the hot inner loop of saving.</summary>
    [Benchmark]
    public int EncodePoints() => BinkPointCodec.Encode(_points).Length;

    /// <summary>Decoding 10,000 points.</summary>
    [Benchmark]
    public int DecodePoints() => BinkPointCodec.Decode(_encodedPoints).Count;

    /// <summary>Exporting the document as SVG.</summary>
    [Benchmark]
    public int ExportSvg() => BinkSvgExporter.Export(_document).Length;
}

/// <summary>Raster export with the built-in rasterizer, used on servers and in tests.</summary>
[MemoryDiagnoser]
public class RasterBenchmarks
{
    private BinkDocument _document = null!;

    /// <summary>Strokes in the document.</summary>
    [Params(200, 2_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup() => _document = Workloads.Document(Strokes);

    /// <summary>Rendering to pixels without encoding, which isolates the rasterizer.</summary>
    [Benchmark]
    public int Rasterize() => BinkPngExporter.Render(_document, new BinkPngExportOptions { Scale = 1f }, out _, out _).Width;

    /// <summary>Full PNG export including compression.</summary>
    [Benchmark]
    public int ExportPng() => BinkPngExporter.Export(_document, new BinkPngExportOptions { Scale = 1f }).Length;
}

/// <summary>Document mutation and undo, which must not copy the document.</summary>
[MemoryDiagnoser]
public class HistoryBenchmarks : IDisposable
{
    private BinkDocument _document = null!;
    private BinkHistoryManager _history = null!;
    private BinkStroke[] _strokes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _document = Workloads.Document(1_000);
        _history = new BinkHistoryManager(_document, limit: 10_000);
        var random = new Random(7);
        _strokes = new BinkStroke[100];
        for (int i = 0; i < _strokes.Length; i++)
        {
            _strokes[i] = new BinkStroke(Workloads.HandwritingStroke(random), new BinkPenBrush());
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _history.Dispose();

    /// <summary>Releases the history manager.</summary>
    public void Dispose()
    {
        _history?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Adding 100 strokes and undoing all of them, the cost of a burst of edits.</summary>
    [Benchmark]
    public int AddAndUndo()
    {
        Guid layer = _document.Layers[0].Id;
        foreach (BinkStroke stroke in _strokes)
        {
            _document.AddElement(stroke with { Id = Guid.NewGuid() }, layer);
        }
        int count = 0;
        while (_history.CanUndo && count < _strokes.Length)
        {
            _history.Undo();
            count++;
        }
        return count;
    }
}

/// <summary>Picking a group and grouping, in a document of loose strokes with some groups among them.</summary>
[MemoryDiagnoser]
public class GroupingBenchmarks
{
    private BinkDocument _document = null!;
    private Guid _member;
    private List<Guid> _hundred = null!;

    /// <summary>Strokes in the document; every tenth group of ten strokes is grouped.</summary>
    [Params(1_000, 10_000)]
    public int Strokes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _document = Workloads.Document(Strokes);
        List<Guid> ids = _document.AllElements.Select(e => e.Id).ToList();
        for (int i = 0; i + 10 <= ids.Count; i += 100) _document.GroupElements(ids.Skip(i).Take(10));
        _member = ids[0];
        _hundred = ids.Skip(ids.Count / 2).Take(100).ToList();
    }

    /// <summary>A click on a grouped stroke: what the select tool does to find the rest of its group.</summary>
    [Benchmark]
    public int ExpandClickToGroup() => BinkDocumentQuery.ExpandToGroups(_document, [_member]).Count;

    /// <summary>Grouping a hundred strokes and taking the group apart again.</summary>
    [Benchmark]
    public bool GroupAndUngroup()
    {
        _document.GroupElements(_hundred);
        return _document.UngroupElements(_hundred);
    }
}

/// <summary>Reading SVG: an icon set's worth of paths.</summary>
[MemoryDiagnoser]
public class SvgImportBenchmarks
{
    private string _svg = null!;

    /// <summary>Paths in the SVG, each a closed shape of curves.</summary>
    [Params(100, 1_000)]
    public int Paths { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var svg = new System.Text.StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 1000\">");
        for (int i = 0; i < Paths; i++)
        {
            float x = i % 40 * 25, y = i / 40 * 25;
            svg.Append(System.FormattableString.Invariant($"<path fill=\"#336699\" transform=\"translate({x} {y})\" d=\"M2 12 C2 6 6 2 12 2 S22 6 22 12 18 22 12 22 2 18 2 12 Z M8 12 A4 4 0 1 0 16 12 A4 4 0 1 0 8 12 Z\"/>"));
        }
        _svg = svg.Append("</svg>").ToString();
    }

    /// <summary>The whole SVG into elements.</summary>
    [Benchmark]
    public int Import() => BinkSvgImporter.Import(_svg).Elements.Count;
}
