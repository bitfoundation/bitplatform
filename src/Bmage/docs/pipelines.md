# Pipelines

An `ImagePipeline` is an immutable, ordered list of operations. Building one does no work; it is a description of an
edit that you can inspect, store, reuse across images and threads, and execute when you are ready.

```csharp
var pipeline = ImagePipeline.Create()
    .AutoOrient()
    .MaxSize(1920, 1920)
    .Sharpen(0.3f);

Console.WriteLine(pipeline);
// Auto orient → Resize(1920x1920, Max) → Sharpen

Console.WriteLine(pipeline.GetOutputSize(new Size(4000, 3000)));
// {Width=1920, Height=1440}   ← computed without touching a single pixel
```

Every builder method returns a new pipeline. The original is unchanged, which is what makes a pipeline safe to hold in a
static field and reuse for every upload.

## Executing

```csharp
using var result = await pipeline.ExecuteAsync(source, new PipelineExecutionOptions
{
    CancellationToken = token,
    Progress = new Progress<double>(p => _percent = p),
});
```

The source is never modified. The returned buffer is yours to dispose.

`ExecuteAsync` yields to the scheduler between operations so a single-threaded WebAssembly UI can repaint. `Execute` is
the synchronous form; use it off the UI thread or for small images.

| Option | Meaning |
| --- | --- |
| `Allocator` | Where intermediate buffers come from. Defaults to the shared pool. |
| `Limits` | Safety limits applied to every buffer allocated during the run. |
| `CancellationToken` | Checked between operations and periodically inside them, typically every few rows. |
| `Progress` | Receives 0 to 1 across the whole run, with each operation's own progress mapped into its slice. |
| `Optimize` | Run the optimiser first. Default true. |
| `YieldBetweenOperations` | Yield to the scheduler between steps. Default true. |
| `YieldCallback` | How to yield, if the default `Task.Yield` is not right for your host. |

Failures are wrapped with context rather than surfacing bare:

```
ImageException: Operation 'Crop' (step 2 of 4) failed: Crop rectangle {X=900,Y=900,Width=100,Height=100}
does not intersect the image (800x600).
```

## The operations

### Geometry

```csharp
pipeline.AutoOrient()                       // apply the EXIF rotation so the image is upright
        .Crop(x, y, width, height)
        .CropToAspect(AspectRatio.Ratio16x9, Anchor.Center)
        .Resize(width, height, mode, filter, anchor, background)
        .MaxSize(1920, 1080)                // shrink to fit, never enlarge
        .Cover(512, 512, Anchor.Top)        // fill exactly, cropping the overflow
        .Rotate(12.5, background)           // arbitrary angle, canvas expands
        .Orient(Orientation.Rotate90)       // lossless, no resampling
        .FlipHorizontal()
        .Skew(10, 0);
```

`AutoOrient` is almost always the first thing you want. Phones record rotation in EXIF rather than rotating the pixels,
so an image that looks right in a photo viewer will appear sideways on a canvas unless it is applied. The browser
decoder applies it during decoding; `AutoOrient` handles the cases where it has not been.

### The six resize modes

For a 1000 × 500 source asked to fit 400 × 400:

| Mode | Result | Behaviour |
| --- | --- | --- |
| `Fit` | 400 × 200 | Scale proportionally to fit inside. The default. |
| `Contain` | 400 × 400 | Like `Fit`, then pad to exactly the target with `background`. |
| `Cover` | 400 × 400 | Scale to fill, then crop the overflow according to `anchor`. |
| `Stretch` | 400 × 400 | Ignore the aspect ratio. |
| `Max` | 400 × 200 | Like `Fit`, but never enlarge a smaller image. |
| `Min` | 800 × 400 | Scale so both dimensions are at least the target. |

`Max` is the right choice for an upload cap: a small image passes through untouched instead of being blown up.

### Resampling filters

| Filter | Use it for |
| --- | --- |
| `Auto` | The default. Lanczos3 for downscaling, bicubic for upscaling. |
| `Box` | Fast area averaging; excellent for large reductions. |
| `Bilinear` | Fast, moderate quality. |
| `Bicubic` | Good general purpose quality. |
| `Lanczos3` | Sharpest downscaling, slowest. |
| `NearestNeighbor` | Pixel art, or when you need hard edges preserved. |

Large reductions automatically get an exact integer box prepass before the chosen filter, which is why a 24 megapixel
thumbnail takes 52 ms rather than 368 ms. You do not opt into this; it is transparent and quality-neutral.

### Smart cropping

A fixed anchor is wrong most of the time. Centre-cropping a portrait to a square routinely cuts off the top of a head,
and centre-cropping a product photo can trim the product.

```csharp
pipeline.SmartCrop(AspectRatio.Square);
```

It scores candidate positions on a small downscaled copy using local detail, colour saturation and skin-like hue, then
picks the best. It is a heuristic, not face recognition. When you do have a detector, hand it the rectangle:

```csharp
var crop = SmartCrop.FindCrop(image, AspectRatio.Square, mustInclude: faceRectangle);
pipeline.Crop(crop.Rectangle);
```

Tuning is available through `SmartCropOptions`: the weights for detail, saturation and skin, how strongly to prefer the
rule of thirds, how much to penalise cutting through busy pixels, and the search resolution.

### Automatic adjustments

```csharp
pipeline.AutoLevels();                    // stretch the tonal range, the most useful single fix
pipeline.AutoLevels(perChannel: true);    // also neutralise a colour cast
pipeline.AutoLevels(strength: 0.5f);      // half way there
pipeline.AutoContrast(targetMean: 0.5f);  // gentler: move the mean luminance towards a target
```

`AutoLevels` ignores the darkest and brightest 0.5% by default, so one hot pixel cannot define the white point, and it
leaves a flat histogram alone rather than amplifying it into noise.

### Adjustments and filters

```csharp
pipeline.Brightness(0.1f).Contrast(0.15f).Saturation(-0.2f).Hue(15)
        .Exposure(0.5f).Gamma(1.2f).Temperature(0.2f).Tint(-0.1f)
        .Vibrance(0.3f).ShadowsHighlights(0.2f, -0.15f).Opacity(0.9f)
        .Grayscale().Sepia(0.8f).Invert().Posterize(6).Threshold(0.5f)
        .Blur(4f).Sharpen(0.5f).Pixelate(12).Noise(0.05f)
        .DenoiseMedian(1).Vignette(0.4f)
        .ColorMatrix(myMatrix);
```

Adjustment amounts are consistently in [-1, 1] with 0 meaning no change, except where a different unit is natural:
`Exposure` is in stops, `Hue` is in degrees, `Gamma` is a positive multiplier, and `Blur` is a radius in pixels.

Filters that take a region only touch that region, which is how redaction works:

```csharp
pipeline.Pixelate(14, new Rectangle(40, 120, 300, 40));
```

## The optimiser

It runs by default and every rewrite is exact.

```csharp
// Three no-ops and two crops.
var pipeline = ImagePipeline.Create()
    .Brightness(0)
    .Crop(10, 10, 200, 200)
    .Crop(5, 5, 100, 100)
    .Contrast(0);

pipeline.Optimize(new Size(400, 400));
// Crop(15,15,100,100)
```

| Rule | Effect |
| --- | --- |
| Drop identities | `Brightness(0)` and friends disappear |
| Merge crops | Two crops become one |
| Merge orientations | Rotate 90 twice becomes rotate 180; two horizontal flips vanish entirely |
| Fuse resize with a following crop | One resample that computes only the surviving region, 2.4× faster |
| Fuse per-pixel runs | One pass over the pixels instead of one per adjustment |

Turn it off with `new PipelineExecutionOptions { Optimize = false }` if you are comparing behaviour, but there is no
reason to in production.

One subtlety worth knowing: fusing per-pixel operations removes the intermediate 8-bit rounding that separate execution
performs, so a fused chain is *more* accurate, not merely faster. Clipping still happens between fused stages, so an
adjustment that blows out a highlight clips the same either way. A long adjustment chain can therefore differ by a few
units per channel depending on whether the optimiser ran, and the fused answer is the better one.

## Previewing at another resolution

```csharp
var preview = pipeline.ForScale(0.25);
```

Every operation rescales itself: crop rectangles, blur radii, pixelation block sizes, stroke widths, font sizes. This is
how the editor previews full-resolution edits on a proxy and gets the same picture, and it is why there is one rendering
path rather than a fast one and an accurate one that drift apart.

## Batch processing

```csharp
var result = await processor.ProcessBatchAsync(items, pipeline, new BatchOptions
{
    MaxConcurrency = 2,
    StopOnFirstError = false,
    ExportOptions = new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.82 },
    Progress = new Progress<BatchProgress>(p =>
    {
        _done = p.Completed;
        if (p.Last.Error is { } error) Log(p.Last.FileName, error);
    }),
}, cancellationToken);

foreach (var item in result.Succeeded) Save(item.Output!);
foreach (var item in result.Failed)    Report(item.FileName, item.Error!);

Console.WriteLine($"{result.SuccessCount} of {result.Results.Count} in {result.Duration.TotalSeconds:0.0}s");
Console.WriteLine($"{result.TotalInputBytes} in, {result.TotalOutputBytes} out");
```

`MaxConcurrency` is a memory dial as much as a speed dial: each worker holds a decoded image, so four workers on 24
megapixel photos means roughly 400 MB of live pixels. Two is a safe default on phones.

One bad file does not lose the rest. Every item comes back with its own outcome, and `StopOnFirstError` is opt-in.

## Custom operations

Implement `IImageOperation`, or `PointOperation` if your operation maps each pixel independently and should fuse with
its neighbours:

```csharp
public sealed class DuotoneOperation : PointOperation
{
    public DuotoneOperation(Rgba32 shadow, Rgba32 highlight) { /* ... */ }

    public override string Name => "Duotone";

    public override void ProcessRow(Span<Vector4> pixels)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            var luminance = Vector4.Dot(pixels[i], new Vector4(0.2126f, 0.7152f, 0.0722f, 0f));
            pixels[i] = Vector4.Lerp(_shadow, _highlight, luminance) with { W = pixels[i].W };
        }
    }
}

pipeline.Apply(new DuotoneOperation(navy, cream));
```

Deriving from `PointOperation` rather than `ImageOperation` means it joins the single-pass run with the adjustments
around it for free. See [extending.md](extending.md) for the other extension points.
