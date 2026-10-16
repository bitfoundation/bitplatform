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
