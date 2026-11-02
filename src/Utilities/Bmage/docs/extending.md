# Extending

Every extension point below is an interface or abstract class. None of them requires modifying or forking the library.

## A custom operation

Implement `PointOperation` when your operation maps each pixel independently. It then joins the single-pass run with the
adjustments around it, so adding one costs nothing extra at runtime.

```csharp
public sealed class DuotoneOperation : PointOperation
{
    private readonly Vector4 _shadow;
    private readonly Vector4 _highlight;

    public DuotoneOperation(Rgba32 shadow, Rgba32 highlight)
    {
        Shadow = shadow;
        Highlight = highlight;
        _shadow = new Vector4(shadow.R, shadow.G, shadow.B, 0) / 255f;
        _highlight = new Vector4(highlight.R, highlight.G, highlight.B, 0) / 255f;
    }

    public Rgba32 Shadow { get; }
    public Rgba32 Highlight { get; }

    public override string Name => "Duotone";

    // ProcessRow only reads its own fields, so rows may run on several threads at once.
    public override bool CanProcessRowsConcurrently => true;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var weights = new Vector4(0.2126f, 0.7152f, 0.0722f, 0f);
        for (var i = 0; i < pixels.Length; i++)
        {
            var alpha = pixels[i].W;
            var luminance = Math.Clamp(Vector4.Dot(pixels[i], weights), 0f, 1f);
            var colour = Vector4.Lerp(_shadow, _highlight, luminance);
            pixels[i] = colour with { W = alpha };
        }
    }
}
```

Operations spread their rows over several threads where .NET has them. The library's own per-pixel operations keep no
state between calls and say so; a custom one runs on one thread unless it overrides `CanProcessRowsConcurrently`, as
above, which it should whenever `ProcessRow` only reads its own fields. Nothing else changes: the pixels are the same on
any number of threads.

For anything that reads neighbouring pixels or changes the size, implement `ImageOperation`:

```csharp
public sealed class BorderOperation : ImageOperation
{
    public BorderOperation(int thickness, Rgba32 colour) { /* ... */ }

    public override string Name => "Border";

    public override Size GetOutputSize(Size input)
        => new(input.Width + _thickness * 2, input.Height + _thickness * 2);

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var output = context.Allocate(source.Width + _thickness * 2, source.Height + _thickness * 2);
        output.Fill(_colour);
        source.CopyTo(output, new Point(_thickness, _thickness));
        output.Metadata = source.Metadata;
        return output;
    }

    // Keeps the border proportional when previewing on a proxy.
    public override IImageOperation ForScale(double scale)
        => scale == 1 ? this : new BorderOperation(Math.Max(1, (int)Math.Round(_thickness * scale)), _colour);
}
```

Three obligations are worth stating plainly:

- **Respect `context.CanMutateSource`.** When false, do not modify the source. `GetOutputBuffer` and `GetMutableCopy` on
  the base class handle the common cases.
- **Check cancellation and report progress** periodically, typically every few rows.
- **Implement `ForScale`** if your operation has any coordinate, radius or size in it. Without it, previews will not
  match exports.

## A custom filter with a kernel

If a convolution is all you need, there is no new type to write:

```csharp
var edges = new ConvolutionFilter(
    kernel: [-1, -1, -1, -1, 8, -1, -1, -1, -1],
    kernelWidth: 3, kernelHeight: 3,
    name: "Edge detect");

pipeline.Apply(edges);
```

Colour transformations that are matrices should be matrices, so they concatenate with their neighbours:

```csharp
var warm = new ColorMatrixOperation(
    ColorMatrix.Concat(ColorMatrix.Saturate(1.1f), ColorMatrix.Scale(1.05f, 1f, 0.95f)),
    "Warm");
```

## A custom codec

```csharp
public sealed class TiffDecoder : IImageDecoder
{
    public IReadOnlyCollection<ImageFormat> Formats => [ImageFormat.Tiff];

    public ImageInfo? Identify(ReadOnlySpan<byte> data) { /* header only */ }

    public ValueTask<ImageBuffer> DecodeAsync(
        ReadOnlyMemory<byte> data, DecodeOptions options,
        IPixelAllocator allocator, CancellationToken cancellationToken)
    {
        options.Limits.ValidateEncodedSize(data.Length);
        // ... validate dimensions against options.Limits before allocating ...
    }
}
```

```csharp
var registry = ImageCodecRegistry.CreateDefault();
registry.AddDecoder(new TiffDecoder());
var processor = new ImageProcessor(registry);
```

Later registrations win, so registering a decoder for a format that already has one replaces it. That is how the browser
codecs take precedence over the managed ones.

## A custom paint

```csharp
public sealed class CheckerPaint : IPaint
{
    public CheckerPaint(int size, Rgba32 a, Rgba32 b) { /* ... */ }

    public Rgba32 GetColor(int x, int y)
        => ((x / _size) + (y / _size)) % 2 == 0 ? _a : _b;
}

canvas.FillPath(path, new CheckerPaint(8, light, dark));
```

## A custom annotation

Derive from `Annotation` and the editor will select, move, reorder, undo and persist it like any built-in kind.

```csharp
public sealed record MeasurementAnnotation : Annotation
{
    public required Vector2 Start { get; init; }
    public required Vector2 End { get; init; }
    public double UnitsPerPixel { get; init; } = 1;
    public ShapeStyle Style { get; init; } = ShapeStyle.Default;

    public override string Kind => "Measurement";

    public override RectangleF GetBounds() => RectangleF.FromLTRB(
        MathF.Min(Start.X, End.X) - 20, MathF.Min(Start.Y, End.Y) - 20,
        MathF.Max(Start.X, End.X) + 20, MathF.Max(Start.Y, End.Y) + 20);

    public override void Render(ImageCanvas canvas, AnnotationRenderContext context)
    {
        if (Style.StrokeColor is not { } colour) return;
        canvas.DrawLine(Start, End, colour, Style.Stroke);

        var distance = Vector2.Distance(Start, End) * UnitsPerPixel;
        if (context.TextRasterizer is { } text)
        {
            var middle = (Start + End) / 2;
            canvas.DrawText($"{distance:0.0}", middle, new TextStyle
            {
                FontSize = 16, FillColor = colour, Alignment = TextAlignment.Center,
            }, text);
        }
    }

    public override Annotation Translate(float dx, float dy)
        => this with { Start = Start + new Vector2(dx, dy), End = End + new Vector2(dx, dy) };

    public override Annotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Start = Start * (float)scale,
        End = End * (float)scale,
        UnitsPerPixel = UnitsPerPixel / scale,   // the measurement itself must not change
        Style = Style.ForScale(scale),
    };

    public override bool HitTest(Vector2 point, float tolerance = 4f)
        => DistanceToSegment(point, Start, End) <= tolerance + Style.Stroke.Width;
}
```

Note `ForScale` on `UnitsPerPixel`: the geometry shrinks with the proxy, so the scale factor has to grow to keep the
reported measurement the same. This is the kind of detail that only shows up when previews and exports disagree.

When the image is cropped, turned, flipped or resized with its overlays attached, each annotation's `Transform` is
called with the affine map from the old output to the new one. The default moves the annotation's centre, scales it by
`ForScale` and turns its `Rotation`, which is right for most shapes; one made of points should map them exactly:

```csharp
    public override Annotation Transform(Matrix3x2 matrix)
    {
        var m = Through(matrix);   // its own rotation, then the image's transform
        return this with { Start = Vector2.Transform(Start, m), End = Vector2.Transform(End, m), Rotation = 0,
                           Style = Style.ForScale(ScaleOf(matrix)), UnitsPerPixel = UnitsPerPixel / ScaleOf(matrix) };
    }
```

## A custom editor tool

```csharp
public sealed class StarTool : IEditorTool
{
    public EditorToolKind Kind => EditorToolKind.Rectangle;   // reuse a kind, or extend the enum

    public Annotation? BuildPreview(ToolDrag drag, EditorToolSettings settings)
        => new PolygonAnnotation
        {
            Points = Star(drag.Start, Vector2.Distance(drag.Start, drag.Current)),
            Style = settings.ToShapeStyle(),
        };

    public Annotation? BuildFinal(ToolDrag drag, EditorToolSettings settings)
        => Vector2.Distance(drag.Start, drag.Current) < EditorTools.MinimumDragDistance
            ? null
            : BuildPreview(drag, settings);
}
```

`BuildPreview` runs on every pointer move, so keep it cheap. `BuildFinal` returning null means the gesture produced
nothing, which is how an accidental click does not litter the document.

## A custom text rasteriser

```csharp
public sealed class MyTextRasterizer : ITextRasterizer
{
    public TextMetrics Measure(string text, TextStyle style) { /* ... */ }
    public VectorPath? GetTextPath(string text, Vector2 origin, TextStyle style) { /* ... */ }
}
```

Pass it to `ImageCanvas.DrawText`, or to `ImageEditSessionOptions.TextRasterizer` so annotations use it.

## A custom allocator

```csharp
public sealed class TrackingAllocator : IPixelAllocator
{
    private readonly IPixelAllocator _inner = PooledPixelAllocator.Shared;
    public long PeakBytes { get; private set; }

    public IMemoryOwner<byte> Rent(int byteCount)
    {
        PeakBytes = Math.Max(PeakBytes, byteCount);
        return _inner.Rent(byteCount);
    }
}

builder.Services.AddBitBmageServices(o => o.Allocator = new TrackingAllocator());
```

Useful for measuring, for enforcing a hard budget, or for pointing at native memory.

## A custom capability provider

```csharp
public sealed class FixedCapabilityProvider : IImageCapabilityProvider
{
    public ValueTask<ImageCapabilities> GetAsync(CancellationToken cancellationToken = default)
        => new(ImageCapabilities.Managed with { EncodableFormats = new HashSet<ImageFormat> { ImageFormat.Png } });
}

builder.Services.AddSingleton<IImageCapabilityProvider, FixedCapabilityProvider>();
```

Handy for tests, and for forcing a conservative configuration.

In a browser, `DecodableFormats` and `EncodableFormats` include what the managed codecs add to the browser's own, and
`NativeDecodableFormats` and `NativeEncodableFormats` say which part is the browser's: only those formats are given to
the browser codecs. A provider that wraps the browser one should change its result with `with`, which keeps them, or copy
them over when it builds a new one; left null, the whole sets are taken as the browser's.

## A custom mask

Derive from `Mask` and write the plain coverage; `Render` applies the feather, inversion and opacity afterwards, and
the mask works with `Masked`, `ApplyMask`, layers and combinations like any built-in one.

```csharp
/// Covers a checkerboard of squares, for a test pattern or a stylised reveal.
public sealed record CheckerMask(float CellSize) : Mask
{
    public override string Kind => "Checker";

    protected override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        var cell = Math.Max(1, (int)CellSize);
        for (var y = 0; y < target.Height; y++)
        {
            var row = target.GetRow(y);
            for (var x = 0; x < target.Width; x++)
                row[x] = ((x / cell) + (y / cell)) % 2 == 0 ? (byte)255 : (byte)0;
        }
    }

    // Keeps the pattern the same size relative to the image when previewed on a proxy.
    protected override Mask ScaleGeometry(double scale) => this with { CellSize = (float)(CellSize * scale) };
}
```

`MaskRenderContext.Source` is the image the mask applies to, for a mask that selects by content, and
`GetCoverageBounds` lets an edit through the mask skip what it cannot reach.

## A custom layer

Derive from `Layer`, produce the layer's pixels in `CompositeCore`, and hand them to `BlendRows`, which applies the blend
mode, opacity and mask, on as many threads as the context allows. Visibility and the mask's rescaling for a proxy are
handled before `CompositeCore` is ever called.

```csharp
/// Film grain laid over the image through the layer's blend mode: Overlay or SoftLight look best.
public sealed record GrainLayer(float Amount) : Layer
{
    public override string Kind => "Grain";

    protected override void CompositeCore(ImageBuffer target, OperationContext context)
    {
        using var grain = ImageBuffer.Create(target.Width, target.Height, new Rgba32(128, 128, 128), context.Allocator);
        new NoiseFilter(Amount, monochrome: true).Apply(grain, context.WithMutation(true));
        BlendRows(target, target.Bounds, context, y => grain.GetRow(y));
    }

    protected override Layer ScaleCore(double scale) => this;
}
```

A layer with geometry of its own overrides `TransformCore` too, so it follows the image through a crop or a turn; a
mask with geometry overrides `TransformGeometry`. Both default to staying put, which is right for something that covers
the whole image.

## Writing your own types as JSON

`BmageJson` writes every built-in operation, mask, layer and annotation. A custom one is written once it has a codec,
and refused with a clear `NotSupportedException` until then, so a document never silently loses a step:

```csharp
var options = new BmageJsonOptions()
    .Add<DuotoneOperation>("acme.duotone",
        (writer, d) => writer.Write("shadow", d.Shadow).Write("highlight", d.Highlight),
        reader => new DuotoneOperation(reader.GetColor("shadow"), reader.GetColor("highlight")))
    .Add<CheckerMask>("acme.checker",
        (writer, m) => writer.Write("cellSize", m.CellSize),
        reader => new CheckerMask(reader.GetSingle("cellSize")));

var json = BmageJson.Serialize(document, options);
var back = BmageJson.DeserializeDocument(json, options);
```

`BmageJsonWriter` and `BmageJsonReader` handle the library's value types: colours, points, sizes, rectangles, point
lists, enums, nested operations, pipelines and masks, and images by reference. Namespacing the type name keeps it
clear of the built-in ones. A mask's or layer's own codec writes only its own properties; the common ones (feather,
inversion, opacity, blend mode and so on) are written for it.

## Background removal

This is an extension point, not an implementation. A model good enough to be useful is several megabytes and would
dominate the bundle for everyone who does not use it, so none is shipped. What it plugs into is a mask: a model
produces a matte, and a `BitmapMask` stretches that matte over the photo whatever its resolution.

```csharp
public interface IBackgroundRemovalProvider
{
    /// Returns a matte: opaque where the subject is, at any resolution.
    ValueTask<ImageBuffer> SegmentAsync(ImageBuffer image, CancellationToken cancellationToken = default);
}

using var matte = await provider.SegmentAsync(photo);

// Cut the subject out...
using var cutOut = ImagePipeline.Create().ApplyMask(new BitmapMask(matte) { Feather = 1.5f }).Execute(photo);

// ...or blur only the background behind it.
using var portrait = ImagePipeline.Create()
    .Masked(new BitmapMask(matte).Invert(), background => background.Blur(12))
    .Execute(photo);
```

Shipping a poor implementation would be worse than shipping none: it would look like a feature and behave like a
disappointment. If you have a model that meets your bundle and latency budget, this is where it plugs in.

A subject on a plain backdrop - a product shot, a logo, a scan - needs no model: `ImagePipeline.RemoveBackground`
floods the backdrop in from the four corners with a `FloodMask` and cuts it out.
