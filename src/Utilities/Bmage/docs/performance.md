# Performance

Every number here was measured, not estimated: in the browser for the WebAssembly table below, and with BenchmarkDotNet
on desktop .NET for the rest. Reproduce the desktop ones with:

```bash
dotnet run --project src/Utilities/Bmage/Tests/Bit.Bmage.Benchmarks -c Release -- --filter "*"
```

**Environment for the desktop tables:** .NET 10.0.13, x64 RyuJIT with AVX2, Windows 11, workstation GC,
BenchmarkDotNet 0.15.8 with two warm-ups and six iterations. Allocations are per call once the array pool is warm. The
tables are for one thread, as the browser runs by default and the benchmarks pin. The
browser is a different machine model altogether; see
[WebAssembly: compile ahead of time](#webassembly-compile-ahead-of-time) for what to expect there.

## WebAssembly: compile ahead of time

By default Blazor WebAssembly runs your .NET code, and this library, on an IL interpreter. Bmage's pixel loops are
written for it: on the interpreter, converting pixels between bytes and floats, and multiplying a vector by a single
float, cost far more than the arithmetic around them, so the loops convert through WebAssembly's own SIMD instructions
and multiply vectors by vectors, with exactly the same results. Where the browser is much faster than .NET can be on
WebAssembly, it does the work: decoding, encoding, and building the editor's working copy of a large photo.

Measured in headless Chromium (Playwright 1.63) on .NET 10.0.11, Release publish, best of up to three runs after a warm-up.
"Before" is the library before this tuning, on the same machine:

| Operation | Interpreter, before | Interpreter | AOT, before | AOT |
| --- | ---: | ---: | ---: | ---: |
| Open a 12 MP photo in the editor (first preview) | 1167 ms | **50 ms** | 179 ms | **49 ms** |
| Brightness slider, one step on a 2048 proxy | 253 ms | **16 ms** | 32 ms | **4 ms** |
| Four adjustments slider, one step on a 2048 proxy | 548 ms | **108 ms** | 79 ms | **24 ms** |
| Export render of a 12 MP photo with four adjustments | 2323 ms | **477 ms** | 363 ms | **112 ms** |
| Upload pipeline on 12 MP: orient, cap at 1920, sharpen | 1624 ms | **754 ms** | 250 ms | **151 ms** |
| Resize 12 MP to 2048, Lanczos3 | 1164 ms | **494 ms** | 183 ms | **109 ms** |
| Resize 1920 × 1080 to 1280, Lanczos3 | 263 ms | **105 ms** | 40 ms | **23 ms** |
| Brightness, 1920 × 1080 | 168 ms | **11 ms** | 22 ms | **3 ms** |
| Gaussian blur, radius 8, 1920 × 1080 | 398 ms | **124 ms** | 55 ms | **25 ms** |
| Unsharp mask, 1920 × 1080 | 531 ms | **179 ms** | 75 ms | **35 ms** |
| Median, radius 1, 1920 × 1080 | 956 ms | **334 ms** | 1058 ms | **27 ms** |
| Rotate 37° (bicubic), 1920 × 1080 | 2974 ms | **908 ms** | 436 ms | **73 ms** |
| Fill 100 translucent rectangles, 1920 × 1080 | 271 ms | **151 ms** | 42 ms | **27 ms** |
| Decode a 12 MP JPEG (browser) | 95 ms | **61 ms** | 77 ms | **61 ms** |
| Encode a 12 MP JPEG (browser) | 137 ms | **99 ms** | 113 ms | **97 ms** |
| Paint a 2048 × 1536 preview | 11 ms | **7 ms** | 8 ms | **6 ms** |

The upload pipeline sharpens at the default radius of 1, which three box passes used to blur 18% less than asked;
the gaussian it now gets accounts for about a tenth of its time.

AOT is still 4 to 12 times faster than the interpreter for work that stays in .NET, so for an application that edits
or processes images heavily, enable it in the project that runs in the browser:

```xml
<PropertyGroup>
  <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

It needs the `wasm-tools` workload (`dotnet workload install wasm-tools`) and three things are worth knowing:

- **It only applies to `dotnet publish`.** `dotnet run` and the Visual Studio debugger always use the interpreter, so
  judge performance on a published build, never in development.
- **The download grows.** The runtime's `dotnet.native.wasm` went from 2.9 MB to 13 MB uncompressed for the demo
  application.
- **Publishing takes minutes rather than seconds**, because every assembly is compiled to WebAssembly.

The relative ordering in the tables below holds under both, so the guidance at the end applies either way.

## Every core

Operations run on one thread unless `MaxDegreeOfParallelism` gives them more. Given ten cores they run 4.7 to 8.5×
faster, with the same pixels: see [threads-and-memory.md](threads-and-memory.md).

## Resizing

Downscaling to 1280px on the long edge, and generating a 200px thumbnail.

| Operation | 1920×1080 | 3840×2160 | 6000×4000 |
| --- | ---: | ---: | ---: |
| Box | 9.8 ms | 16.5 ms | 63.1 ms |
| Bilinear | 10.6 ms | 20.6 ms | 65.0 ms |
| Bicubic | 13.4 ms | 25.4 ms | 76.1 ms |
| Lanczos3 | 16.0 ms | 40.0 ms | 82.2 ms |
| Thumbnail to 200px (Auto) | 3.3 ms | 8.1 ms | 22.6 ms |

Allocations stay between 23 KB and 185 KB regardless of source size, because pixel buffers are pooled and the resampler
works one row at a time rather than building a full intermediate.

### The box prepass

Thumbnailing a 24 megapixel photo originally took **368 ms**, making it the slowest operation in the suite. That is
absurd for the most common thing anyone does with an uploaded image, and the cause was structural: shrinking by 30× with
Lanczos3 gives the filter a support of 90 source pixels, so 180 taps per axis per output pixel.

An exact integer box average is precisely the low-pass that wide kernel was approximating. Doing that first, to within
2× of the target, and finishing with the requested filter:

| | Before | After |
| --- | ---: | ---: |
| 6000×4000 to 200px | 368 ms | **52 ms** |
| Allocated | 244 KB | **23 KB** |
| 3840×2160 to 200px | 52 ms | **19 ms** |

Both columns come from the run made when the prepass landed, on .NET 10.0.11 with a shorter BenchmarkDotNet job. Later
tuning has since brought the thumbnail down to the 22.6 ms and 8.1 ms in the table above.

`ResamplingQualityTests` guards the quality: a ramp stays linear and monotonic to within 4 levels of its analytic value,
solid colours survive reductions of 2× through 50× to within 1 level, a fine checkerboard averages to mid grey instead of
aliasing, transparent edges keep their colour, and content stays aligned to within a pixel.

## Operations

| Operation | 1920×1080 | 3840×2160 | Allocated |
| --- | ---: | ---: | ---: |
| Crop | 0.06 ms | 0.28 ms | 152 B |
| Rotate 90 (lossless) | 1.8 ms | 27.2 ms | 120 B |
| Brightness (one pass) | 1.3 ms | 5.1 ms | 288 B |
| Four adjustments, fused | 17.2 ms | 67.4 ms | 4.8 KB |
| Four adjustments, unfused | 18.4 ms | 75.2 ms | 4.5 KB |
| Gaussian blur, radius 8 | 25.3 ms | 99.5 ms | 1.4 KB |
| Unsharp mask | 27.3 ms | 107 ms | 1.2 KB |
| Rotate 37° (resampled) | 65.6 ms | 236 ms | 512 B |

An arbitrary rotation costs far more than any other geometric operation, and almost all of that is the sampler,
which runs once per output pixel. Measured on a 12 megapixel image rotated by 37 degrees:

| `WarpSampling` | Throughput | Use when |
| --- | ---: | --- |
| `NearestNeighbor` | 105 MP/s | Pixel art, or a preview that will be re-rendered |
| `Bilinear` | 95 MP/s | Interactive rotation of a large image |
| `Bicubic` (default) | 70 MP/s | Final output |

A rotation by a multiple of 90 degrees never samples at all and runs at roughly 350 MP/s, so prefer
`OrientationOperation` whenever the angle allows it. The editor stays responsive at any setting because previews
render against a downscaled proxy; the full-resolution pass happens once, on export.

### Editing tools

`EditingToolBenchmarks`, one thread, on another machine than the tables above (an i9-10900 on Windows 10, .NET 10), so
compare rows within this table: the vibrance row is an existing per-pixel adjustment measured in the same run. Rows
marked * were measured again after the optimisations below, in a run where vibrance took 35.1 ms and 169 ms.

| Operation | 1920×1080 | 3840×2160 | Allocated |
| --- | ---: | ---: | ---: |
| Trim bounds * | 1.3 ms | 5.2 ms | 0 B |
| Canvas: 40px border | 1.6 ms | 8.4 ms | 152 B |
| Whites/blacks (tables) | 3.6 ms | 13.4 ms | 3.6 KB |
| Auto white balance | 15.4 ms | 64.0 ms | 528 B |
| Mask expand by 10px | 27.9 ms | 94.6 ms | 12 KB |
| Duotone * | 28.4 ms | 141 ms | 8.0 KB |
| Vibrance (reference) | 36.8 ms | 156 ms | 352 B |
| Colorize * | 50.6 ms | 187 ms | 16 KB |
| High pass, radius 3 | 64.3 ms | 250 ms | 1.3 KB |
| Remove a plain background (corner flood) | 66.8 ms | 272 ms | 0.9 KB |
| HSL, two bands | 93.7 ms | 445 ms | 0.7 KB |
| Straighten 5° | 122 ms | 410 ms | 688 B |
| Paint layer, 20 long strokes * | 134 ms | 284 ms | 2.3 MB |
| Motion blur 20px, horizontal or 30° | 142 ms | 677-738 ms | 480 B |
| A fill clipped to the paint layer * | 200 ms | 579 ms | 2.3 MB |
| Isolated group: the paint layer and a multiplied fill * | 225 ms | 675 ms | 2.3 MB |

A clipped layer and an isolated group each composite into a buffer the size of the image and blend it back, so they
cost about a full-image blend more than their layers do on their own; a clipping stack blends back, and switches its
transparency, only over the rows and columns its base reaches, which for a logo is a small part of the image. Trimming tests four pixels at a time with
`Vector128`, and the gradient map and colorize mix whole `Vector4`s rather than building `Vector3`s and scaling them by
single floats, which took them on the WebAssembly interpreter from 128, 459 and 351 ms in an earlier run to 44, 152 and
105 ms (below), all now cheaper there than vibrance.

### WebAssembly: editing tools

The rows above in the browser, from the performance page's "Run the editing tools", best of two runs of its best of
three: headless Chrome 156, a Release publish of the demo on .NET 10 with and without ahead-of-time compilation, on the
i9-10900. The page's first benchmark, whose interpreter and AOT numbers are published above, took about twice as long
on this machine (189, 21, 289, 240 and 427 ms on the interpreter against 108, 11, 118, 125 and 203; 52, 7, 47, 52 and
75 ms with AOT against 24, 3, 23, 24 and 36), so compare these rows with each other and with the desktop column, not
with the table at the top.

| Operation | Interpreter | AOT | Desktop .NET |
| --- | ---: | ---: | ---: |
| Canvas: 40px border | 3 ms | 1 ms | 1.6 ms |
| Trim bounds | 44 ms | 1 ms | 1.3 ms |
| Whites and blacks | 21 ms | 7 ms | 3.6 ms |
| Auto white balance | 43 ms | 15 ms | 15.4 ms |
| Duotone | 152 ms | 28 ms | 28.4 ms |
| Vibrance, for reference | 161 ms | 26 ms | 35.1 ms |
| Colorize | 105 ms | 46 ms | 50.6 ms |
| Expand a selection by 10px | 349 ms | 91 ms | 27.9 ms |
| High pass, radius 3 | 366 ms | 66 ms | 64.3 ms |
| Remove a plain background | 714 ms | 102 ms | 66.8 ms |
| HSL, two bands | 758 ms | 87 ms | 93.7 ms |
| Motion blur 20px | 618 ms | 267 ms | 142 ms |
| Paint layer, 20 long strokes | 1383 ms | 179 ms | 134 ms |
| Straighten 5° | 1684 ms | 88 ms | 122 ms |
| A fill clipped to the paint layer | 2313 ms | 327 ms | 200 ms |
| Isolated group: the paint layer and a multiplied fill | 3281 ms | 372 ms | 225 ms |

Compiled ahead of time, nine of the sixteen rows run as fast as desktop .NET on one thread or faster; the rest take 1.3
to 1.9 times as long, painting, background removal, the clipped and grouped layers, motion blur and whites and blacks,
except the selection expand at 3.3 times. On the interpreter, straightening (which shares rotation's sampler), painting
and the layer stacks are the slow rows: in the editor they run on the proxy, and as a draft while a control is
dragged, as every edit does.

To measure again, publish the demo twice and point the opt-in browser test at each:

```bash
dotnet publish src/Utilities/Bmage/Demo/Server -c Release -p:RunAOTCompilation=false -o pub-interpreter
dotnet publish src/Utilities/Bmage/Demo/Server -c Release -o pub-aot       # AOT is on for a Release publish
dotnet pub-aot/Bit.Bmage.Demo.Server.dll --urls http://127.0.0.1:5311 &
BMAGE_E2E_MEASURE=1 BMAGE_E2E_HOST_URL=http://127.0.0.1:5311 BMAGE_E2E_MEASURE_OUT=aot.tsv \
    dotnet test --project src/Utilities/Bmage/Tests/Bit.Bmage.Tests.E2E --filter "FullyQualifiedName~MeasureTheEditingTools"
```

The file holds the page's first benchmark too, as calibration against the published rows.

Whites and blacks are a curve, so they run through 256-entry tables like brightness. Colorize became a table of
lightness, a 1025-entry piecewise linear lookup that is exact at the one kink of the conversion; HSL skips every pixel
whose two neighbouring bands are not adjusted before converting it. Both still pay for the floating point row the
non-separable adjustments share, which is most of what vibrance costs too. Motion blur costs about one sample per pixel
of the streak, whatever its angle; the editor previews it on the proxy, or a draft of it while the slider moves, and
caps it at 120 pixels there.

Two things to read from this:

- **Fusing adjustments is worth about 1.1×.** Adjustments that map each channel from itself (brightness, contrast,
  gamma and the like) run through 256-entry tables whether fused or not, so only one that mixes channels, such as
  saturation, works in floating point; fusing saves the other passes over the pixels.
- **Rotating by a right angle is 9 to 37× cheaper than an arbitrary angle**, because it moves pixels rather than
  resampling them. The optimiser routes multiples of 90° to the lossless path automatically.

Blur is O(n) in the image and independent of radius: it is three box passes approximating a gaussian, so radius 40 costs
the same as radius 4. Below a radius of about 2, where boxes of whole-pixel widths would miss the requested radius by
more than 10%, a sampled gaussian of at most 13 taps runs instead.

## Pipelines

4000 × 3000 source.

| Pipeline | Optimised | Unoptimised | Gain |
| --- | ---: | ---: | ---: |
| Resize then crop | **25.8 ms** | 62.2 ms | 2.41× |
| Auto-orient, cap at 1920, sharpen | 101 ms | 104 ms | none |

The second row is the honest one: that pipeline has nothing to fuse, so the optimiser changes nothing beyond noise.
It is left on by default because the cases where it helps, help a lot.

Resize followed by crop is the big win. Rendering only the surviving region is bit-identical to rendering everything and
cropping, because the filter kernels are still computed for the full output, so the discarded pixels are simply never
resampled.

Allocation for the whole pipeline is about 238 KB against a 48 MB image, which is the pooling working.

## Codecs

| Operation | 1024×768 | 3840×2160 | Allocated (4K) |
| --- | ---: | ---: | ---: |
| PNG decode | 8.7 ms | 88 ms | 37 MB |
| JPEG decode | 7.4 ms | 77 ms | 14 MB |
| JPEG encode, quality 85 | 11.5 ms | 122 ms | **6.7 MB** |
| PNG encode, level 1 | 19.2 ms | 198 ms | 98 MB |
| PNG encode, level 6 | 68.0 ms | 720 ms | 46 MB |

PNG compression level matters more than anything else here: level 6 is 3.6× slower than level 1 for a modest size gain.
Level 1 is usually the right default for an interactive preview, and the library's default of 6 is right for a final
export.

### Two memory fixes that came from these numbers

**The JPEG encoder allocated 116 MB for a 4K image.** It converted the whole image into three floating point planes up
front, twelve bytes per pixel, which is enough to fail outright on a phone. Streaming it one MCU row at a time:

| | Before | After |
| --- | ---: | ---: |
| Allocated, 3840×2160 | 116.5 MB | **6.7 MB** |
| Time | 183 ms | 175 ms |

17× less memory and slightly faster, because the working set now fits in cache.

**The PNG encoder buffered the entire compressed stream** before writing it as one IDAT chunk. PNG permits any number of
IDAT chunks, so emitting them incrementally halved the allocations: 177 MB to 98 MB at level 1, 78 MB to 46 MB at level 6
for a 4K image.

## Drawing

1920 × 1080 canvas.

| Operation | Time | Allocated |
| --- | ---: | ---: |
| Stroke 100 arrows | 8.0 ms | 405 KB |
| Fill 100 ellipses | 9.5 ms | 601 KB |
| Fill 100 rectangles | 10.6 ms | 119 KB |
| Fill a 480 segment bezier path | 71.6 ms | 7.2 MB |

The rasteriser computes coverage one row at a time, only across the columns the shape covers in that row, and never
allocates a full-size coverage buffer, so drawing cost scales with the shape's area rather than the canvas. Where a
pixel is fully covered, an opaque colour is written as is and a translucent one over an opaque pixel goes through
three 256-entry tables.

### The active edge table

The complex path case originally took **1075 ms**, because every sub-scanline walked every edge in the path: with five
sub-samples per row over 1080 rows and thousands of flattened segments, the cost was the product of the two rather than
the sum.

Sorting the edges by their top and maintaining an active set as the scanline advances:

| | Before | After |
| --- | ---: | ---: |
| 480 segment bezier path | 1075 ms | **107 ms** |
| Stroke 100 arrows | 34.2 ms | 24.4 ms |

Both columns come from the run made when the active edge table landed, on .NET 10.0.11 with a shorter BenchmarkDotNet
job. Later tuning has since brought the path down to 71.6 ms and the arrows to 8.0 ms, as in the table above.

10× on the complex case. This matters more than a synthetic benchmark suggests, because text is exactly this shape: a
line of glyphs is hundreds of small contours, and every text annotation pays it.

## Interactive editing

The editor edits through a proxy no larger than 2048px on its longest edge and replays the edit at full resolution on
export. For a 24 megapixel photo that is a 25× reduction in the pixels touched per preview, which is the difference
between a slider that tracks your finger and one that does not.

On top of that, a preview never recomputes what an earlier one already did, and the results are identical:

- The pipeline is split after its last step that the optimiser never combines with a neighbour (a blur, a sharpen, a
  rotation or any custom operation), and that part is kept while the document keeps those operations. A slider after a
  blur re-runs only what follows the blur: about 16 ms per step on the interpreter instead of the blur's 190 ms.
- With annotations present, the pipeline's output is kept, so dragging one of 20 annotations takes about 20 ms on the
  interpreter, and drawing a new shape over them about 2 to 5 ms per frame.
- The editor renders one preview at a time and always the latest state, so fast input skips intermediate values
  instead of queueing renders. A full preview that has become pointless, because a drag went back to drafts or the
  pointer moved again during the refinement of a resting drag, is cancelled rather than finished.
- When a full preview still takes longer than 25 ms, dragging shows drafts at half the proxy's resolution (a blur
  slider step drops from 192 ms to 41 ms on the interpreter), and the full preview follows when the pointer rests.

Because operations rescale themselves, the preview and the export are the same edit at two resolutions rather than two
code paths that can disagree.

History stores descriptions, not pixels. A test asserts that 100 undo steps on a 6000 × 4000 image stay under 1 MB;
storing pixel snapshots would need roughly 4.8 GB.

## Guidance

**Cap dimensions before anything else.** Every later operation costs proportionally less.

```csharp
ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f)
```

**Group adjustments together.** Consecutive per-pixel operations fuse into one pass; separating them with a geometric
operation prevents that.

**Prefer right angles.** `Rotate(90)` takes the lossless path; `Rotate(90.5)` resamples the whole image.

**Keep batch concurrency low.** Each worker holds a decoded image. Two is a safe default; four on 24 megapixel photos
means roughly 400 MB of live pixels.

**Dispose buffers.** A 4000 × 3000 image is 48 MB of pooled memory. `using` returns it immediately.

**Use object URLs, not data URLs.** Base64 inflates by a third and forces the bytes through the DOM as a string.

**Lower the PNG level for previews.** Level 1 for something on screen for a moment, level 6 for the file the user keeps.
