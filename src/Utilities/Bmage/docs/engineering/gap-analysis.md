# Bmage gap analysis

This is an engineering document, not a user guide: the guides are the files directly under `docs/`, which the demo
website and its MCP server embed. It records what Bmage could do when this round of work started, where it fell short
of a complete browser image editing library, and the order in which the gaps are being closed. The live status of every
requirement is in [coverage-matrix.md](coverage-matrix.md).

## 1. Baseline (2026-10-22, commit `c16e5b6f85`)

| Check | Result |
| --- | --- |
| `dotnet build Bit.Bmage.Tests -f net10.0` | 0 warnings, 0 errors |
| `Bit.Bmage.Tests` (engine, net10.0) | 969 passed, 0 failed, 36 s |
| `Bit.Bmage.Tests.E2E` (Chromium, Debug demo) | see section 9 |
| Browsers available to Playwright locally | Chromium 1187, Firefox 1543, WebKit 2359 |

Targets: `net8.0`, `net9.0`, `net10.0`. One package, `Bit.Bmage`, depending only on `Microsoft.AspNetCore.Components.Web`.
Trimmable and AOT-compatible (repository-wide `IsAotCompatible`).

## 2. What Bmage is

Both an image processing API and an embeddable editor, layered so the editor is optional:

```
Editor   (Bit.Bmage.Editor.*)   BmageEditor, toolbar, panel, canvas, tools, strings
Browser  (Interop, Codecs, Capabilities, Input, Output)   bit-bmage.js, Web Worker codec pool
Engine   (everything else)       ImageBuffer, operations, pipelines, masks, layers, drawing, text,
                                 annotations, documents, history, managed PNG/JPEG/BMP/GIF codecs, JSON
```

The engine is managed C#, not Canvas 2D. That is a deliberate, documented decision (`docs/architecture.md`): the same
pixels on every browser and on a server, headless replay of an edit, exact proxy previews through `ForScale`, and no
canvas taint or colour-space surprises. The browser is used where it is genuinely better: WebP and AVIF codecs, the
editor's proxy resample (`createImageBitmap`), text shaping, clipboard, camera, files and pointer capture. This round
keeps that architecture; section 6 explains why a Canvas or WebGL engine was not adopted.

### Image lifecycle as implemented

1. **Import** - `ImageLoader` (`IBrowserFile`, bytes, streams, data/blob/http URLs, clipboard, paste listener, camera)
   sniffs magic bytes, never the extension, and decodes through `ImageProcessor`: managed PNG/JPEG/BMP/GIF first,
   browser codecs (in a worker pool) for WebP, AVIF and anything the managed decoders reject. `ImageLimits` and
   `MaxSourceBytes` are checked before allocation. EXIF is read; `AutoOrient` applies the orientation.
2. **Edit** - `ImageEditSession` owns the original, a display-sized proxy, and an `EditHistory` of immutable
   `ImageDocument` values (pipeline + layers + annotations). Previews render on the proxy, serialised and cancellable,
   with staged caches split at optimiser barriers.
3. **Render** - `DocumentRenderer`: pipeline, then layers through W3C blend modes and masks, then annotations.
4. **Export** - `ImageExporter` / `ImageProcessor.EncodeAsync`: PNG, JPEG, BMP managed; WebP and AVIF through the
   browser with verified (not assumed) format support; `MaxFileSize` search; metadata policy (`Remove`,
   `StripSensitive`, `Preserve`).
5. **Dispose** - buffers are pooled and returned on `Dispose`; the session releases buffers only when no render reads
   them (`WhenIdle`); JS objects are released through handles.

## 3. Capability inventory and assessment by category

Ratings: **Strong** (complete for the category's common needs, tested), **Partial** (useful but with named gaps),
**Missing**.

| Category | Rating | What exists | Gaps found |
| --- | --- | --- | --- |
| A. Import & decoding | Strong | All sources above; format sniffing; managed + browser codecs; EXIF/ICC/XMP read; first-frame policy for GIF/APNG; SVG refused by design; limits; CORS errors explained | No host allow-list for URL loading; the editor imports one file and cannot add a file as a layer |
| B. Geometry | Partial | Crop, aspect crop, smart crop, six resize modes, 90° orientations, arbitrary rotate, skew, affine, perspective, straighten-quad | **No canvas extend/reduce with anchor**; **no trim** of transparent or uniform borders; **no straighten** (rotate + crop to the inscribed rectangle); editor has no resize, canvas size, or arbitrary-angle UI |
| C. Colour & tone | Partial | Brightness, contrast, exposure, gamma, saturation, vibrance, hue, temperature, tint, shadows/highlights, levels, curves, colour matrix, grayscale, sepia, invert, threshold, posterize, auto levels, auto contrast, histograms | **No HSL / per-colour adjustment**, **no channel mixer / black-and-white mixer**, **no white balance** (picked neutral or automatic), **no whites/blacks**, **no colorize**, no gradient map / duotone, no histogram in the editor |
| D. Filters | Partial | Gaussian blur, unsharp-mask sharpen, pixelate, noise/grain, median, vignette, convolution (emboss, edge detect presets), colour matrices, masked application | **No motion blur**, **no high-pass**, no gradient map / duotone, no halftone, no layer effects (drop shadow, outline, glow) |
| E. Layers | Partial | Image, fill, adjustment layers; 16 blend modes; opacity, masks, visibility, lock; cached composites | No paint layer; **no duplicate**; editor panel cannot change opacity, blend mode or order, or add an image layer; history size estimate ignores layers; no groups or clipping |
| F. Selection | Partial | Rectangle, ellipse, polygon/lasso, path, brush, gradient, luminance and colour range, bitmap (alpha/luminance) masks; combine, invert, feather, opacity; masked edits; alpha cut-out | **No magic wand / flood selection**, **no expand / contract / border**, no one-step uniform background removal, no selection tool in the editor |
| G. Painting | Partial | Freehand vector annotation (smoothed), shapes, arrows, polygons, gradient paints in the canvas API, pointer pressure captured | **No raster brush with hardness, no eraser, no eyedropper, no bucket fill**; pressure unused; clone/heal absent |
| H. Text & shapes | Strong | Text (family, size, weight, italic, alignment, baseline, line height, letter spacing, wrapping, stroke, background box), callouts, steps, shapes with dash/caps/joins, rotation, z-order, lock, hit testing, browser or TrueType rasteriser | Editor text editing is basic (new text only); covered later under N |
| I. History | Strong | Immutable documents, linear history with labelled entries, transient drags, go-to, reset, depth limit, KB-sized entries | `EstimatedSize` ignores layers and masks; no before/after compare in the editor |
| J. Export | Strong | PNG, JPEG, BMP, WebP, AVIF; quality; size budget search; alpha flattening for JPEG; metadata policies; blob, bytes, stream, download, clipboard; JSON document with external image references | **No self-contained project file** that carries the original and layer images; editor has no project save/open |
| K. Performance | Strong | Proxy and draft previews, staged caches, fused point ops, exact resize+crop fusion, box prepass, streamed JPEG/PNG, parallel rows, worker codecs, memory estimates, budgeted allocator, benchmarks | New operations must keep to the same standards (streamed rows, no full float copies) |
| L. Compatibility | Partial | Capability probing by encoding a pixel; documented support matrix; worker fallback | **E2E runs in Chromium only**, although Firefox and WebKit are installable through the same Playwright |
| M. Public API | Strong | Headless and editor APIs, typed options, cancellation, progress, typed exceptions, DI, JSON, extension points | New features must arrive in both the pipeline builder and the JSON codecs |
| N. Editor UI | Partial | Tools: select, pan, crop (aspect presets, handles), draw, line, arrow, rectangle, ellipse, highlight, redact, text, step; rotate/flip; adjust sliders; filter presets; layers & history panels; export panel; zoom/fit; keyboard shortcuts; RTL; theming; localisation | No compare, no straighten/resize/canvas/trim UI, no histogram, no brush/eraser/eyedropper/fill/wand tools, no layer controls, no project save/open |
| O. Accessibility | Partial | Toolbar/tablist roles, roving tabindex, labelled controls, live region for messages, keyboard nudging of annotations, focus styles | New tools and panels must keep parity; numeric alternatives needed for straighten/resize/canvas |
| P. Security & privacy | Strong | Local processing, metadata stripped by default, SVG refused, magic-byte sniffing, limits before allocation, JSON reader validates | Project archives need zip-bomb protections (entry count, sizes, path names) |
| Q. Extensibility | Strong | Operations, masks, layers, annotations, codecs, paints, text rasterisers, tools, allocators, capability providers, JSON codecs | - |

## 4. Competitor and platform comparison (summary)

| Capability | Photoshop / GIMP / Krita | Browser editors (Photopea, Pixlr, Filerobot, TOAST UI) | Bmage at baseline |
| --- | --- | --- | --- |
| Canvas size with anchor, trim | Yes | Yes (Photopea), partial elsewhere | Missing |
| Straighten with auto-crop | Yes (ruler / crop rotate) | Filerobot, Pixlr | Missing (rotate exists) |
| HSL / per-colour | Yes | Photopea, Pixlr | Missing |
| Channel mixer, B&W mixer | Yes | Photopea | Missing (colour matrix exists) |
| White balance picker | Camera Raw, GIMP | Pixlr | Missing |
| Gradient map / duotone | Yes | Photopea, Canva | Missing |
| Motion blur, high pass | Yes | Photopea | Missing |
| Magic wand, grow/shrink selection | Yes | Photopea, Pixlr | Missing (colour range exists) |
| Raster brush, eraser, eyedropper, bucket | Yes | Most | Missing |
| Non-destructive adjustment layers, masks, blend modes | Yes | Photopea | Present |
| Project file with layers | PSD / XCF / KRA | PSD (Photopea), JSON (Filerobot) | JSON with external images only |
| Headless, server-replayable edits | Scripting only | No | Present, a differentiator |

Native browser APIs considered: Canvas 2D (`filter`, `globalCompositeOperation`), `ImageBitmap`/`createImageBitmap`,
`OffscreenCanvas`, Web Workers, WebGL2, WebGPU, `ImageDecoder` (WebCodecs), `CompressionStream`. Their use is assessed
in section 6.

## 5. Prioritised requirements

| # | Requirement | Priority | Depends on | Batch |
| --- | --- | --- | --- | --- |
| 1 | Canvas extend/reduce with anchor and background; content-bounds analysis for trim; straighten with inscribed crop | P1 | - | 1 |
| 2 | HSL per-colour, channel mixer (incl. monochrome), gradient map / duotone, white balance (neutral + auto), whites/blacks, colorize | P1 | - | 2 |
| 3 | Motion blur, high-pass | P1/P2 | - | 3 |
| 4 | Magic wand (flood) mask, mask expand/contract/border, uniform background removal | P2 | - | 4 |
| 5 | Paint layer: raster brush with hardness, pressure, eraser; bucket fill; eyedropper | P2 | 4 (flood for bucket) | 5 |
| 6 | Layer duplicate; editor layer controls (opacity, blend, order, add image); history size counts layers | P2 | - | 6 |
| 7 | Editor: compare, transform panel (straighten, resize, canvas, trim), histogram, new adjustments and filters, new tools | P1/P2 | 1-6 | 7 |
| 8 | Self-contained project file (`.bmage` zip) with validation and limits; editor save/open | P2 | - | 8 |
| 9 | Cross-browser E2E: Firefox and WebKit through `BMAGE_E2E_BROWSER` | P0 (verification) | - | 9 |
| 10 | Accessibility pass on everything added | P1 | 7 | 7, 10 |
| 11 | Editor selection: rectangle, ellipse, lasso and magic wand, combined by Shift and Alt, outlined with marching ants; make transparent, fill, crop to, copy to a layer, invert, select all | P2 | 4 | 11 |
| 12 | Edit a selected annotation (text, size, rotation, opacity, order, lock) from the panel | P1 | - | 11 |

Deferred with rationale (see the matrix for each):

- **Clone / healing brush** (P3): a clone stamp is feasible, but healing that produces useful results needs
  Poisson/patch-match reconstruction; shipping a poor one is worse than none.
- **Layer groups and clipping masks** (P3): need a group compositing model (isolated groups, pass-through) and a
  serialisation format change; masks plus adjustment layers cover most needs today.
- **Layer effects (shadow, glow, outline)** (P3): achievable with masks and blur; worth a design of its own.
- **Halftone** (P3): niche; expressible as a custom `PointOperation` today.
- **AI background removal / segmentation** (P3): the `BitmapMask` extension point exists; no model is bundled (size,
  licensing, network). Uniform-background removal ships in batch 4 instead.
- **ICC colour management** (P3): profiles are read and preserved but pixels are treated as sRGB; documented honestly.
- **Animated GIF/APNG/WebP editing** (P3): first frame only, documented.
- **Editable history serialisation** (P3): documents serialise; the timeline does not.

## 6. Architecture assessment and decisions

**Keep the managed engine.** A Canvas 2D or WebGL engine would be faster for some filters in the browser, but it
would (a) give different pixels per browser and none on a server, breaking headless replay and the exactness of proxy
previews, (b) need a second implementation of every operation for the server, and (c) bring canvas tainting and
colour-space conversions into the core. The measured costs that matter interactively are already handled: the proxy
resample is native, previews run at proxy or draft scale, codecs run in workers. AOT gives 4-12× on WebAssembly.

**WebGPU / WebGL** were not adopted: no measured interactive bottleneck at proxy resolution justifies a second backend
with context-loss handling and a GPU/CPU parity test burden. Revisit if a benchmark of a specific filter at proxy size
exceeds the frame budget on target hardware.

**New features follow the existing shape**: every edit is an immutable operation, mask, layer or annotation with
`GetOutputSize`, `ForScale`, `IsIdentity`, a JSON codec and a pipeline builder method. Content-dependent sizes (trim)
are expressed as an analysis step that yields a concrete `CropOperation`, because `IImageOperation.GetOutputSize` must
not need pixels: documents, memory estimates and annotation placement all rely on it.

**Painting** is a layer of vector brush strokes (`PaintLayer`), not a mutable bitmap: undo stays KB-sized and strokes
rescale for the proxy.

**Project files** are a ZIP of the JSON document and PNG images (`System.IO.Compression`, already in the BCL and
available on WebAssembly), not a new binary format.

## 7. Performance and memory risks

| Risk | Mitigation |
| --- | --- |
| Flood fill on large images | Scanline flood with a byte visited map (1 byte/pixel), no recursion |
| Mask expand/contract | Exact Euclidean distance transform, O(n), two float rows per pass, applied only when asked |
| Paint layers with many strokes | Strokes rasterised once per composite into the layer's bounds; composite cached by the session like other layers |
| Motion blur with long distances | Cost is O(n·distance); distance clamped, region-limited, cancellable per row |
| Project archives | Limits on entry count (counted before .NET opens the archive), per-entry and total uncompressed size, each image's dimensions and all images' pixels together before decoding; only images the document uses are decoded |
| Owned layer images | Kept until another image is opened, as the history may return to them; repeated copies to a layer grow memory until then (documented) |

## 8. Accessibility, compatibility and security risks

- Every new editor control needs an accessible name, keyboard operation and a numeric alternative to pointer input.
- Firefox and WebKit differ in canvas encoding support (AVIF, WebP), clipboard and pointer events; the E2E suite must
  run there to find out rather than assume.
- Zip archives are untrusted input: path traversal is irrelevant (nothing is written to disk) but entry names are
  validated, and sizes are bounded before allocation.

## 9. Baseline E2E

Chromium (a Debug build of the demo, the bundled Chromium being absent so the installed Chrome or Edge ran): 71 of 71
passed in 6 minutes 48 seconds. The full log is in the coverage matrix.

## 10. Status after this round

Every requirement in section 5 except the deferred ones has been implemented and verified; the coverage matrix names
the evidence for each. Defects found along the way and fixed:

- **Annotations did not follow geometric edits.** `architecture.md` said `IGeometricOperation` kept annotations
  attached to the content, but nothing used it: crop or turn the image in the editor and an arrow pointed elsewhere.
  Overlays now move with the image (`ImageDocument.WithPipeline(pipeline, keepOverlaysAttached: true)`).
- **Placed text could not be edited** in the built-in editor, which always wrote the placeholder "Text".
- **The MCP surface test failed** once two guides passed the 15,000 character read limit, because the tool's and the
  resource's truncation notices differ by design.
- **Firefox:** the test simulating a paste cannot work there (Firefox empties a synthetic event's clipboard data), and
  one page's first click hit Playwright's 30 second default while the Debug build hydrated.

Found while building the new features and fixed before they shipped: a motion blur weighting that added half a tap at
each end, a layer row that deselected when clicked, a string parameter Razor bound literally, and an outline traced
from a stale preview.

An independent review of the editor, selection and project batch found, and this round fixed:

- **A project could ask for far more memory than its size.** Decoded pixels were not bounded together, and every image
  the manifest listed was decoded whether used or not: a one-colour 8192 × 8192 PNG takes a few hundred kilobytes
  and decodes to 256 MB, deflate compressing such data about a thousandfold. Images are now decoded only when the document uses them, against
  `BmageProjectOptions.MaxTotalPixels`, and the archive's files are counted before .NET builds its directory.
- **Some malformed projects escaped as the wrong exception** (invalid manifest JSON, a non-numeric version, a damaged
  archive); all are `ImageDocumentFormatException` now.
- **Undo left the selection where the undone edit had moved it**, so a selection made before a turn covered other
  pixels once the turn was undone. Undo, redo, the history panel and reset now carry it through the change in
  geometry.
- **Actions that measure the image first applied the result to whatever the image had become**: a trim, a crop to the
  selection or a copy to a layer, with an undo or a slider in between. They now stop when the document changed, and
  undo, redo and reset wait while the editor is busy.
- **A full render did not hold back the release of what it read**, so disposing the session, or opening another image,
  during an export could return the original's pixels to the pool mid-read. Releases now wait for full renders already
  running (`ReviewRegressionTests.DisposingDuringAnExportKeepsTheOriginalUntilTheExportIsDone` fails without it).
- Smaller: images leaked when a project opened into an editor whose image-loaded handler threw or loaded another image,
  or when the editor closed while an image layer loaded; the Retouch demo disposed images a render in flight could still
  read; the magic wand's tolerance had no control; Ctrl+Shift+I opened the developer tools as well as inverting the
  selection where the browser lets a page keep the key.
- Weak tests strengthened: the copy-to-layer E2E test now shows the copy holds the pixels in place (it passed with an
  empty layer before), and the contour test asserts the saddle's outcome exactly.

Known and not fixed: on Interactive Server, the session disposes a replaced preview on a thread-pool thread, which the
circuit's thread may still be reading for the canvas, the histogram or the eyedropper. The effect is one wrong frame or
sample, corrected by the next render; WebAssembly, where .NET runs on one thread, is unaffected. Delaying the disposal
would cost a second preview's memory everywhere.

WebKit: Playwright's WebKit build for Windows has no `OffscreenCanvas`. The library falls back to the page's thread
there as designed; two worker tests now check that fallback and report the worker path as inconclusive, and a test
helper that used `OffscreenCanvas` itself now uses a canvas element.

Measured: the new operations' costs are in `docs/performance.md`, Editing tools; colorize and HSL were optimised after
the first measurement showed them several times a reference adjustment's cost, which brought colorize to 1.5 and HSL to
2.5 times it.
