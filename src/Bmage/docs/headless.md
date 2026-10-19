# Headless processing

Nothing in this library requires a component. The editor is one consumer of the engine, not the engine itself.

## In the browser, without the editor

```razor
@inject ImageLoader Loader
@inject ImageExporter Exporter

@code {
    async Task<EncodedImage> MakeThumbnail(IBrowserFile file)
    {
        using var image = await Loader.LoadAsync(file);
        using var thumb = await ImagePipeline.Create()
            .AutoOrient()
            .MaxSize(320, 320)
            .Sharpen(0.3f)
            .ExecuteAsync(image);

        return await Exporter.EncodeWithFallbackAsync(
            thumb,
            [ImageFormat.WebP, ImageFormat.Jpeg],
            new ImageExportOptions { Quality = 0.8 });
    }
}
```

This is the shape most applications need: shrink an upload before it is sent, so the network and the server never see the
full-resolution original.

## On a server, with no browser at all

```csharp
using Bit.Bmage;   // AddBitBmageHeadlessServices lives here

builder.Services.AddBitBmageHeadlessServices(options =>
{
    options.Limits = ImageLimits.Default with { MaxPixels = 30_000_000 };
});
```

```csharp
app.MapPost("/upload", async (IFormFile file, ImageProcessor processor) =>
{
    // Cap the upload before reading it; IFormFile reports its length up front.
    if (file.Length > 20 * 1024 * 1024) return Results.BadRequest("Too large.");
    using var buffer = new MemoryStream();
    await file.CopyToAsync(buffer);
    var bytes = buffer.ToArray();

    // Reject before decoding anything.
    var info = processor.Identify(bytes);
    if (info is null) return Results.BadRequest("Not a supported image.");
    if ((long)info.Width * info.Height > 30_000_000) return Results.BadRequest("Too large.");

    var thumbnail = await processor.ProcessAsync(
        bytes,
        ImagePipeline.Create().AutoOrient().MaxSize(400, 400),
        new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.8 });

    return Results.File(thumbnail.Data, thumbnail.MimeType);
});
```

Only the managed codecs exist here: PNG, JPEG, BMP and GIF for decoding, PNG, JPEG and BMP for encoding. Asking for WebP
throws `ImageCapabilityException` with a message saying so.

## The pieces

| Type | Purpose |
| --- | --- |
| `ImageBuffer` | The pixels. Pooled; dispose it. |
| `ImagePipeline` | An immutable list of operations. |
| `ImageProcessor` | Decode, process, encode, and batch. Registered directly by `AddBitBmageHeadlessServices`; in a browser app get one from `ImageProcessorFactory.GetAsync()` instead. |
| `ImageCanvas` | Immediate-mode drawing. |
| `ImageCodecRegistry` | Which decoders and encoders are available. |
| `ImageDocument` | A non-destructive edit: pipeline plus annotations. |
| `DocumentRenderer` | Renders a document against a source image. |
| `EditHistory` | Undo and redo over documents. |
| `FontTextRasterizer` | Text from a font file, no browser needed. |

## Drawing without a UI

`ImageCanvas` is a complete drawing surface. This composes a social card from scratch:

```csharp
using var canvas = ImageCanvas.Create(1200, 630, new Rgba32(17, 19, 24));

canvas.DrawImage(photo, new RectangleF(700, 0, 500, 630));

// Fade the photo into the panel.
var fade = new LinearGradientPaint(
    new Vector2(700, 0), new Vector2(900, 0),
    [(0f, new Rgba32(17, 19, 24, 255)), (1f, new Rgba32(17, 19, 24, 0))]);
canvas.FillPath(new PathBuilder().AddRectangle(new RectangleF(700, 0, 200, 630)).Build(), fade);

canvas.FillRoundedRectangle(new RectangleF(60, 60, 160, 40), 20, new Rgba32(37, 99, 235));
canvas.DrawText("Release notes", new Vector2(60, 200), new TextStyle
{
    FontSize = 56,
    Bold = true,
    FillColor = Rgba32.White,
    MaxWidth = 580,
}, textRasterizer);

var png = await canvas.ExportAsync(ImageExportOptions.Png);
```

Available on the canvas: `FillPath`, `StrokePath`, `ErasePath`, `DrawLine`, `DrawRectangle`, `FillRectangle`,
`DrawRoundedRectangle`, `FillRoundedRectangle`, `DrawEllipse`, `FillEllipse`, `FillCircle`, `FillPolygon`,
`DrawPolyline`, `DrawArrow`, `DrawImage`, `DrawText`, plus `Save`, `Restore`, `Translate`, `Scale`, `Rotate`, `ClipTo`
and `ApplyPipeline`.

Paths come from `PathBuilder`: `MoveTo`, `LineTo`, `QuadraticTo`, `CubicTo`, `ArcTo`, `Close`, `AddRectangle`,
`AddRoundedRectangle`, `AddEllipse`, `AddPolygon`, `AddPolyline`. Curves are flattened adaptively.

Fills take a colour or an `IPaint`: `SolidPaint`, `LinearGradientPaint`, `RadialGradientPaint`, or your own.

## Text without a browser

```csharp
var font = FontFile.Load(await File.ReadAllBytesAsync("Inter-Regular.ttf"));
var rasterizer = new FontTextRasterizer()
    .Register("Inter", font)
    .Register("Inter", boldFont, bold: true);

var metrics = rasterizer.Measure("Hello", style);
canvas.DrawText("Hello", position, style, rasterizer);
```

TrueType and OpenType fonts with `glyf` outlines are supported, including composite glyphs, `cmap` formats 0, 4, 6 and
12, horizontal metrics and kerning pairs. CFF (PostScript) outlines are not, and the error says so explicitly rather
than producing blank text.

Layout is left-to-right with kerning and optional word wrapping. Complex script shaping and bidirectional reordering are
not performed; use `BrowserTextRasterizer` in a browser for those.

## Analysing an image

```csharp
var stats = ImageStatistics.Measure(image);

stats.Luminance.Mean;              // exposure
stats.Luminance.GetPercentile(0.5);
stats.Red.Minimum;                 // per-channel range
stats.AverageColor;                // a placeholder or letterbox colour
stats.IsGrayscale;
stats.HasTransparency;

var palette = ImageStatistics.GetDominantColors(image, count: 5, ignoreNearWhiteAndBlack: true);
foreach (var c in palette) Console.WriteLine($"{c.Color.ToHex(false)} {c.Percent:0.#}%");
```

Measuring samples on a regular grid by default, which keeps a 24 megapixel photo well under a frame while staying
indistinguishable from a full pass. Pass `maxSamples: 0` to read every pixel.

## Placeholders for progressive loading

```csharp
var placeholder = ImagePlaceholder.Create(image, columns: 4, rows: 4);

var token = placeholder.ToCompactString();   // 67 characters for the default 4x4 grid, store it beside the row
placeholder.AspectRatio;                     // reserve layout space before the image loads
placeholder.AverageColor;                    // a single-colour fallback

// Later, in the browser or on the server:
var restored = ImagePlaceholder.Parse(token, width, height);
using var blurred = restored!.Render(400, 300);   // smooth, no image request
var css = restored.ToCssBackground();             // or render it with CSS gradients alone
```

This is the low-quality image placeholder pattern: show the right colours and shape immediately, then swap in the real
image. A placeholder is small enough to return from a list endpoint without thinking about it.

## Non-destructive editing without the editor

```csharp
var document = ImageDocument.Create(source)
    .AddOperation(new CropOperation(100, 100, 800, 600))
    .AddOperation(new BrightnessAdjustment(0.1f))
    .AddAnnotation(new ArrowAnnotation { Start = a, End = b });

var history = new EditHistory(document);
history.Push(document.AddAnnotation(label), "Add label");
history.Undo();

using var rendered = new DocumentRenderer(textRasterizer)
    .Render(history.Current, source);
```

A document is a value. Serialise it, send it to a server, or apply it to a different image. Rendering never modifies the
source.

## Batch

```csharp
var result = await processor.ProcessBatchAsync(items, pipeline, new BatchOptions
{
    MaxConcurrency = Environment.ProcessorCount / 2,
    ExportOptions = new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.82 },
});
```

On a server you can afford more concurrency than in a browser, but each worker still holds a decoded image. Size it
against your memory budget, not your core count alone.

## Custom codecs

Register your own decoder or encoder and it takes precedence over the built-ins for its formats:

```csharp
var registry = ImageCodecRegistry.CreateDefault();
registry.AddDecoder(new MyTiffDecoder());
registry.AddEncoder(new MyTiffEncoder());
var processor = new ImageProcessor(registry);
```

See [extending.md](extending.md).
