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
        _shadow = new Vector4(shadow.R, shadow.G, shadow.B, 0) / 255f;
        _highlight = new Vector4(highlight.R, highlight.G, highlight.B, 0) / 255f;
    }

    public override string Name => "Duotone";

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

## Background removal

This is an extension point, not an implementation. A model good enough to be useful is several megabytes and would
dominate the bundle for everyone who does not use it, so none is shipped.

The intended shape:

```csharp
public interface IBackgroundRemovalProvider
{
    ValueTask<ImageBuffer> RemoveBackgroundAsync(ImageBuffer image, CancellationToken cancellationToken = default);
}
```

Wrap it as an `ImageOperation` and it composes with everything else:

```csharp
public sealed class RemoveBackgroundOperation : ImageOperation
{
    public override string Name => "Remove background";
    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
        => _provider.RemoveBackgroundAsync(source, context.CancellationToken).GetAwaiter().GetResult();
}
```

Shipping a poor implementation would be worse than shipping none: it would look like a feature and behave like a
disappointment. If you have a model that meets your bundle and latency budget, this is where it plugs in.
