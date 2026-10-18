# Architecture

## The layers

```
Bit.Bmage.Editor      Components, tools, theming, localisation
        │
Bit.Bmage      Browser codecs, capabilities, loading, export, one JS module
        │
Bit.Bmage.Core        Pixels, geometry, colour, operations, pipelines, drawing,
                        text, annotations, documents, history, managed codecs
```

`Core` has no dependency on Blazor, ASP.NET Core or JavaScript. It is the engine, and it runs anywhere .NET runs.
`Blazor` adds the browser: the codecs only a browser has, capability probing, and the plumbing for files, clipboard,
camera and downloads. `Editor` is the user interface, and it is optional in the strong sense that nothing below it knows
it exists.

An application that only needs to shrink an upload installs `Bit.Bmage` and never pays for the editor.

## The central idea

An edit is a **description**, not a mutation.

```
ImageBuffer (original pixels, never modified)
        │
ImageDocument
   ├── ImagePipeline  ── an ordered list of IImageOperation
   └── Annotations    ── a list of immutable annotation records
        │
DocumentRenderer  ──►  ImageBuffer (the result, owned by the caller)
```

Because the document is a value, and every operation is an immutable record that knows its own output size and how to
rescale itself, several things fall out for free rather than being features that had to be built:

- **Undo costs nothing.** A history entry is the document, which is a list of small objects. A hundred undo steps on a
  24 megapixel photo take kilobytes, not gigabytes. There is a test asserting exactly that.
- **Previews are cheap.** `document.ForScale(0.25)` returns the same edit expressed for a quarter-size image. The
  editor previews on a proxy and replays the original document at full resolution on export. There is one code path, not
  two.
- **Any step can be changed.** `pipeline.Replace(2, newCrop)` re-edits the third operation without touching the rest.
- **Edits are portable.** A document can be stored, sent to a server, or applied to a different image.

## Pixels

`ImageBuffer` is the single working representation: tightly packed RGBA32, straight (non-premultiplied) alpha, row-major.
That is deliberately the same layout as the browser's `ImageData`, so moving pixels across the boundary is a copy rather
than a conversion.

Buffers come from an `IPixelAllocator`. The default pools through `ArrayPool<byte>`, which is why a resize of a 24
megapixel image allocates a couple of hundred kilobytes rather than a hundred megabytes: the large buffers are rented and
returned, and only the small bookkeeping is new. Disposing a buffer returns its memory immediately.

Alpha is straight in storage but **premultiplied for every operation that mixes pixels**: resampling, blurring, box
reduction, warping and compositing. Mixing straight alpha is the classic source of dark fringes around transparent edges.
There is a test that catches exactly that regression.

## Operations

```csharp
public interface IImageOperation
{
    string Name { get; }
    Size GetOutputSize(Size inputSize);       // without touching pixels
    ImageBuffer Apply(ImageBuffer source, OperationContext context);
    IImageOperation ForScale(double scale);   // the same edit at another resolution
    bool IsIdentity { get; }                  // can the optimiser drop it
}
```

`GetOutputSize` is what lets a pipeline report its result size before doing any work. `ForScale` is what makes proxy
previews exact rather than approximate. `IsIdentity` is what lets a slider dragged back to zero cost nothing.

Two specialisations carry most of the weight:

- **`PointOperation`** maps each pixel independently, working on rows of `Vector4` in [0,1]. Runs of them execute in a
  single pass over the image. Colour matrices concatenate algebraically; curves compose their lookup tables.
- **`IGeometricOperation`** exposes the affine matrix it applies, which is how annotations stay attached to image content
  when a geometric edit is inserted before them.

## The pipeline optimiser

Every rewrite is exact, and the contract is stated precisely because "roughly the same" is not good enough for a library:

| Rule | Guarantee |
| --- | --- |
| Drop identity operations | Bit identical |
| Merge consecutive crops | Bit identical |
| Merge consecutive orientations | Bit identical |
| Fuse a resize with a following crop | Bit identical |
| Fuse runs of per-pixel operations | Identical except that intermediate 8-bit rounding is removed, so the fused result is *more* accurate |

The resize and crop fusion is worth explaining because it is the least obvious. Rendering a sub-region of a resize would
normally change the filter kernels, because the kernels depend on the output extent. The resampler instead accepts a
destination rectangle that may sit partly outside the destination buffer, including a negative origin. Kernels are
always built for the full output, and only the visible part is written. Rendering a window is therefore bit-identical to
rendering everything and cropping, which is what makes the fusion safe. It is 2.4× faster on the benchmark because the
discarded pixels are never resampled.

Clipping behaviour is preserved across per-pixel fusion: values are still clamped to [0,1] between fused stages, matching
CSS filter chain semantics, so an adjustment that blows out a highlight clips exactly as it would alone. Only the
rounding differs. Tests assert both halves of that claim.

## Resampling

A single separable resampler implements every resize mode, because each mode reduces to the same description: a canvas
size, a source rectangle and a destination rectangle. The vertical pass runs first and produces one floating point row at
a time, so no full-size intermediate is allocated.

Large reductions get an exact integer box prepass. Shrinking by 30× with Lanczos would otherwise need 180 taps per axis
per output pixel. A box average is precisely the low-pass the wide kernel was approximating, so doing it first and
finishing with the requested filter is both far faster and visually equivalent. Measured: 368 ms to 52 ms for a 24
megapixel thumbnail. `docs/performance.md` has the numbers and `ResamplingQualityTests` has the proof that quality held.

## Drawing

A scanline rasteriser with analytic horizontal coverage and five vertical sub-samples per row. Coverage is computed one
row at a time and handed to a callback, so filling a shape never allocates a full-size coverage buffer.

Strokes are converted to fill outlines by a stroker that emits a quad per segment plus separate join and cap shapes.
Overlaps are harmless because the result is filled with the non-zero rule. Dash patterns are applied by splitting the
polyline before stroking.

## Text

Two rasterisers, because the right answer depends on where you are:

- **`FontTextRasterizer`** parses TrueType and OpenType fonts with `glyf` outlines: `cmap` formats 0, 4, 6 and 12,
  composite glyphs, `hmtx` metrics and `kern` pairs. It produces real vector outlines, works headlessly, and is tested
  against a purpose-built font committed to the repository.
- **`BrowserTextRasterizer`** hands text to the platform's own text engine and gets pixels back. System fonts, emoji,
  right-to-left scripts and complex shaping all work without shipping a font file.

The trade is stated rather than hidden: the browser rasteriser returns pixels, so it cannot give you outlines to stroke
or transform arbitrarily. The font rasteriser can, but does not perform complex script shaping.

## The JavaScript boundary

One module, `bit-bmage.js`, and it exists only for what the browser platform alone can do:

- Decoding and encoding WebP, AVIF and the browser's own JPEG and PNG paths
- Capability probing
- Clipboard, camera, downloads, the File System Access API and object URLs
- Text measurement and rendering with the platform text engine
- Non-passive touch listeners, which Blazor's event handling cannot register

No image processing happens there. Pixel and file bytes cross as raw streams, never base64: a JS call returns a small
handle object, .NET reads a tiny JSON descriptor from it and streams the payload, then releases it. A 24 megapixel image
costs one buffer copy instead of a 33% inflated string.

## Memory

The concern is treated as a design constraint rather than a later optimisation.

- Pixel buffers are pooled and explicitly disposed.
- The resampler allocates per row, not per image.
- The JPEG encoder works one MCU row at a time; building full float planes cost 116 MB for a 4K image, streaming costs
  6.7 MB.
- The PNG encoder emits incremental IDAT chunks rather than buffering the whole compressed stream.
- Interactive editing runs on a proxy; the full-resolution buffer is touched once, on export.
- History stores descriptions, not pixels.
- `ImageLimits` rejects absurd dimensions before any allocation happens.

## Extension points

Every one of these is an interface or an abstract class you can implement without modifying the library: `IImageOperation`
and `PointOperation` for processing, `IImageDecoder` and `IImageEncoder` registered in `ImageCodecRegistry`, `IPaint` for
fills, `ITextRasterizer` for text, `Annotation` for new annotation kinds, `IEditorTool` for new editor tools,
`IPixelAllocator` for memory strategy, and `IImageCapabilityProvider` for environment detection. See
[extending.md](extending.md).

## What is deliberately not here

- **Background removal** ships as an architecture, not an implementation. A usable model is several megabytes and would
  dominate the bundle. The extension point exists so a provider can be plugged in; shipping a poor one would be worse
  than shipping none.
- **Web Workers** are not used yet. The benchmark evidence says the wins are smaller than the complexity for the common
  operations, and `ImageCapabilities` already reports whether workers and `OffscreenCanvas` are available so the
  architecture does not preclude it.
- **SVG rasterisation** is refused rather than attempted. Rendering untrusted SVG can execute scripts and fetch remote
  resources; the decoder returns a clear error explaining that instead.
- **Animated GIF and APNG** decode their first frame only. Full animation support is a different problem than the one
  this library is solving.
