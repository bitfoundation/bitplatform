# Architecture

## The layers

```
Bit.Bmage   Editor:  components, tools, theming, localisation (Bit.Bmage.Editor.*)
                │
            Browser: codecs, capabilities, loading, export, one JS module
                │
            Engine:  pixels, geometry, colour, operations, pipelines, drawing,
                     text, annotations, documents, history, managed codecs
```

The engine has no dependency on Blazor, ASP.NET Core or JavaScript, and it runs anywhere .NET runs. The browser layer,
shipped in the same package, adds the codecs only a browser has, capability probing, and the plumbing for files,
clipboard, camera and downloads; nothing in the engine calls into it. `Editor` is the user interface, and it is optional
in the strong sense that nothing below it knows it exists.

An application that only needs to shrink an upload never pays for the editor: it never links `bit-bmage-editor.css`,
and a trimmed WebAssembly publish drops the editor's code along with everything else the application does not use.

## The central idea

An edit is a **description**, not a mutation.

```
ImageBuffer (original pixels, never modified)
        │
ImageDocument
   ├── ImagePipeline  ── an ordered list of IImageOperation, some applied through masks
   ├── Layers         ── image, fill and adjustment layers, each with a blend mode, opacity and mask
   └── Annotations    ── a list of immutable annotation records
        │
DocumentRenderer  ──►  ImageBuffer (the result, owned by the caller)
```

Because the document is a value, and every operation, mask, layer and annotation is an immutable record that knows its
own output size and how to rescale itself, several things fall out for free rather than being features that had to be
built:

- **Undo costs nothing.** A history entry is the document, which is a list of small objects. A hundred undo steps on a
  24 megapixel photo take kilobytes, not gigabytes. There is a test asserting exactly that.
- **Previews are cheap.** `document.ForScale(0.25)` returns the same edit expressed for a quarter-size image. The
  editor previews on a proxy and replays the original document at full resolution on export. There is one code path, not
  two.
- **Any step can be changed.** `pipeline.Replace(2, newCrop)` re-edits the third operation without touching the rest.
- **Edits are portable.** `BmageJson` writes a document as JSON and reads it back as the same edit, so it can be stored,
  sent to a server and replayed there on the original file, or applied to a different image.

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
- **`IGeometricOperation`** exposes the affine matrix it applies. `ImagePipeline.TryGetTransform` composes them into the
  pipeline's map from source to output coordinates, and `ImageDocument.WithPipeline(pipeline, keepOverlaysAttached: true)`
  moves every annotation, layer and pixel mask by the difference between the old map and the new one, so they stay on
  the content they mark through a crop, a quarter turn, a flip, a resize, a canvas change or a straighten, and through
  re-editing any of them. The editor's geometric tools all go through it. Points map exactly; an upright rectangle maps
  exactly under any transform that keeps rectangles upright; otherwise a shape's centre follows, its size scales and
  its rotation turns, and text is never mirrored. Plain `AddOperation` keeps its documented meaning: overlays stay in
  output coordinates.

## Masks and layers

A `Mask` is a description, like an operation: a shape, a gradient, a tone or colour range, brush strokes, another
image's matte, or a combination, in pixels or in fractions of the image. It renders into a `MaskBuffer`, one byte of
coverage per pixel, for the image it applies to, and rescales itself for a proxy like everything else.

`MaskedOperation` applies any size-keeping operation through a mask. A per-pixel operation goes through
`PointOperationExecutor.RowProcessor`, the very code an unmasked run uses for each row, so a full mask is bit-identical
to no mask, and only over the bounds the mask can reach. Anything else runs whole and is blended back. It is an
optimiser barrier, which is also where the editor's preview cache splits.

A `Layer` composites onto the pipeline's output through a `BlendMode` (the W3C Compositing and Blending Level 1
modes), an opacity and an optional mask. The edit session caches the composite separately from the pipeline output, so
annotation edits never composite the layers again and layer edits never re-run the pipeline.

A `GroupLayer` is a layer holding a list of layers, so the document stays one tree of immutable values and every edit,
a group's included, is still one new document. `LayerCompositor` walks each list the same way: a run of layers with
`ClipToLayerBelow` after a base is drawn with it into a buffer of its own, the base at full strength, the clipped
layers as if it were opaque, then limited to its alpha and blended with its mode and opacity; a pass-through group
composites its list in place and, when faded or masked, mixes the result with a copy of what was beneath; an isolated
group composites its list into a transparent buffer and blends that. Groups nest at most ten deep, which bounds the
buffers held at once. The layer image cache reaches into groups, so a group's image layers are prepared once like the
document's own.

## Threads and workers

**In .NET**, operations whose rows are independent split them into bands (`ParallelRows`) over
`MaxDegreeOfParallelism` threads, each band with its own scratch rows and shared read-only kernels and tables, so the
pixels are identical on any number of threads. Blurs stay sequential: their running sums would change if split. The
default is one thread, which suits a server already running requests side by side and the browser, where .NET has only
the page's thread; an application processing one image at a time sets more.

**In the browser**, decoding, encoding and the editor's proxy resize run in a small Web Worker pool, with buffers
transferred rather than copied, and fall back to the page's thread wherever workers cannot run. See
[threads-and-memory.md](threads-and-memory.md).

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
  composite glyphs, `hmtx` metrics, `kern` pairs and the OpenType layout tables (GDEF, GSUB, GPOS). It shapes text -
  Arabic and Persian joining, ligatures, contextual alternates, kerning, mark positioning - orders mixed-direction text
  with the Unicode Bidirectional Algorithm (checked against Unicode's own conformance files), and falls back across the
  registered fonts character by character. It produces real vector outlines, works headlessly, and is tested against a
  purpose-built font and a Persian one committed to the repository.
- **`BrowserTextRasterizer`** hands text to the platform's own text engine and gets pixels back. System fonts, emoji,
  right-to-left scripts and complex shaping all work without shipping a font file.

The trade is stated rather than hidden: the browser rasteriser returns pixels, so it cannot give you outlines to stroke
or transform arbitrarily. The font rasteriser can, and draws the same on a server as in a browser, but it does not
shape scripts that reorder glyphs within a word (Indic, Southeast Asian) or attach them cursively (Nastaliq).

## The JavaScript boundary

One module, `bit-bmage.js`. It does what only the browser platform can do, and the few things the browser does far
faster than .NET running on WebAssembly:

- Decoding and encoding WebP, AVIF and the browser's own JPEG and PNG paths, in Web Workers where the browser allows
- Building the editor's working proxy of a large photo with the browser's high quality resampler (`createImageBitmap`).
  On WebAssembly's interpreter that takes about 50 ms for a 12 megapixel photo, where the managed resampler takes
  500 ms. The proxy only shapes the preview: export replays the edit on the original pixels in the managed engine, so
  the exported file is the same in every browser and on a server.
- Capability probing
- Clipboard, camera, downloads, the File System Access API and object URLs
- Text measurement and rendering with the platform text engine
- Pointer capture, non-passive touch and wheel listeners, and keeping pointer moves that are not part of a drag from
  reaching .NET at all, which Blazor's event handling cannot do

Everything else stays in C#: operations, pipelines, drawing, annotations and the managed codecs, which produce the same
pixels wherever they run.

Pixel and file bytes never cross as base64. On WebAssembly the module is called through `[JSImport]` with views of .NET
memory (`BrowserPixels`), so a 12 megapixel image is copied exactly once in each direction, straight between a .NET
buffer and a typed array, with no 48 MB array allocated for the call. Over a Blazor Server circuit the browser is in
another process: a JS call returns a small handle object, .NET reads a tiny JSON descriptor from it and streams the
payload, then releases it. On Server the managed codecs decode and encode PNG, JPEG, BMP and GIF on the server itself,
so those pixels never cross the network; the browser is used for WebP and AVIF, and for a file the managed decoder
rejects.

## Memory

The concern is treated as a design constraint rather than a later optimisation.

- Pixel buffers are pooled and explicitly disposed.
- The resampler allocates per row, not per image.
- The JPEG encoder works one MCU row at a time; building full float planes cost 116 MB for a 4K image, streaming costs
  6.7 MB.
- The PNG encoder emits incremental IDAT chunks rather than buffering the whole compressed stream.
- Interactive editing runs on a proxy; the full-resolution buffer is touched once, on export.
- Blurs and sharpening stream rows through their passes, keeping only the rows each pass still needs instead of float
  copies of the whole image.
- History stores descriptions, not pixels.
- `ImageLimits` rejects absurd dimensions before any allocation happens.
- Masks are one byte a pixel, and a masked adjustment touches only the rows its mask reaches.
- Parallel bands each rent a few rows of scratch, never a copy of the image.

A caller can also predict and bound it: `ImagePipeline.EstimateMemory` gives a run's peak before it starts, a
`BudgetedPixelAllocator` enforces a ceiling, and a streamed batch admits images by their estimated need. See
[threads-and-memory.md](threads-and-memory.md).

## Extension points

Every one of these is an interface or an abstract class you can implement without modifying the library: `IImageOperation`
and `PointOperation` for processing, `Mask` for selections, `Layer` for compositing, `IImageDecoder` and `IImageEncoder`
registered in `ImageCodecRegistry`, `IPaint` for fills, `ITextRasterizer` for text, `Annotation` for new annotation
kinds, `IEditorTool` for new editor tools, `IPixelAllocator` for memory strategy, `IImageCapabilityProvider` for
environment detection, and `BmageJsonOptions.Add` for writing any of your own types as JSON. See
[extending.md](extending.md).

## Content-dependent sizes

`IImageOperation.GetOutputSize` must be answerable from the input size alone: documents report their size, overlays
are placed and memory is estimated before any pixel is read. Trimming plain borders depends on the pixels, so it is an
analysis, `ContentBounds.Find`, whose result is recorded as an ordinary `CropOperation` that replays exactly. Automatic
corrections that keep the size (auto levels, auto white balance) are operations: they measure the image they are
applied to, which on a proxy preview is the proxy.

## Painting

A `PaintLayer` holds brush strokes, not pixels: each stroke is its points, pen pressures, size, colour, opacity and
hardness. Strokes are stamped as round dabs into a coverage buffer, kept at the larger value where dabs overlap so a
stroke has one even opacity, then laid over the layer (or, for the eraser, taken out of it). Undo stays kilobytes, and a
proxy preview paints the same strokes scaled. While a stroke is drawn the editor shows it as a line over the kept
preview rather than compositing the layers on every pointer move, and paints it properly when the pointer lifts.

## What is deliberately not here

- **Semantic background removal** ships as an architecture, not an implementation. A usable segmentation model is
  several megabytes and would dominate the bundle; `BitmapMask` takes any model's matte. What ships is the case a
  model is not needed for: `ImagePipeline.RemoveBackground` floods a plain backdrop in from the corners.
- **Healing and content-aware fill.** Results worth using need patch-based reconstruction; a clone stamp that only
  copies would be easy to ship and disappointing to use.

- **The .NET engine in a Web Worker.** Codec work runs in workers, but operations stay on .NET's thread. Moving them
  would mean a second .NET runtime in a worker, tens of megabytes and a second startup, to free a thread that previews
  on a proxy already keep responsive. A WebAssembly app built with threads gets the engine on several cores through
  `MaxDegreeOfParallelism` instead.
- **SVG rasterisation** is refused rather than attempted. Rendering untrusted SVG can execute scripts and fetch remote
  resources; the decoder returns a clear error explaining that instead.
- **Animated GIF and APNG** decode their first frame only. Full animation support is a different problem than the one
  this library is solving.
