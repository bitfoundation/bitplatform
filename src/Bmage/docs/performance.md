# Performance

Every number here was measured with BenchmarkDotNet, not estimated. Reproduce them with:

```bash
dotnet run --project src/Bmage/Tests/Bit.Bmage.Benchmarks -c Release -- --filter "*"
```

**Environment for the tables below:** .NET 10.0.11, x64 RyuJIT with AVX2, Windows 11, workstation GC. Short job:
one launch, one warmup, three iterations. In the browser these numbers only hold with AOT compilation; see
[WebAssembly: compile ahead of time](#webassembly-compile-ahead-of-time) before reading anything else here.

## WebAssembly: compile ahead of time

By default Blazor WebAssembly runs your .NET code, and this library, on an IL interpreter. Tight pixel loops are the
worst case for an interpreter: every `Vector4` operation that RyuJIT turns into one SIMD instruction becomes a separate
interpreted step. Measured on a 1920×1080 image in Chrome, best of three after a warmup, against desktop .NET on the
same machine:

| Operation | Interpreter | AOT | Desktop .NET |
| --- | ---: | ---: | ---: |
| Resize to 1280, Lanczos3 | 505 ms | **56 ms** | 34 ms |
| Brightness | 406 ms | **27 ms** | 21 ms |
| Four adjustments, fused | 1008 ms | **63 ms** | 48 ms |
| Gaussian blur, radius 8 | 789 ms | **88 ms** | 57 ms |
| Unsharp mask | 1267 ms | **119 ms** | 86 ms |
| Rotate 37° (bicubic) | 8627 ms | **594 ms** | 246 ms |

The interpreter is 10 to 35× slower than desktop .NET; AOT is 1.2 to 2.4×. A Debug build measured the same as a
Release build on the interpreter, so the build configuration is not the issue. Enable AOT in the project that runs in
the browser:

```xml
<PropertyGroup>
  <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

It needs the `wasm-tools` workload (`dotnet workload install wasm-tools`) and three things are worth knowing:

- **It only applies to `dotnet publish`.** `dotnet run` and the Visual Studio debugger always use the interpreter, so
  judge performance on a published build, never in development.
- **The download grows.** The runtime's `dotnet.native.wasm` went from 2.9 MB to 13 MB uncompressed in the measurement
  above.
- **Publishing takes minutes rather than seconds**, because every assembly is compiled to WebAssembly.

The relative ordering in the tables below holds under both, so the guidance at the end applies either way.

## Resizing

Downscaling to 1280px on the long edge, and generating a 200px thumbnail.

| Operation | 1920×1080 | 3840×2160 | 6000×4000 |
| --- | ---: | ---: | ---: |
| Box | 15.0 ms | 35.4 ms | 127 ms |
| Bilinear | 17.4 ms | 40.2 ms | 114 ms |
| Bicubic | 21.9 ms | 49.0 ms | 124 ms |
| Lanczos3 | 26.8 ms | 67.7 ms | 134 ms |
| Thumbnail to 200px (Auto) | 6.6 ms | 19.2 ms | 52.4 ms |

Allocations stay between 22 KB and 180 KB regardless of source size, because pixel buffers are pooled and the resampler
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

`ResamplingQualityTests` guards the quality: a ramp stays linear and monotonic to within 4 levels of its analytic value,
solid colours survive reductions of 2× through 50× to within 1 level, a fine checkerboard averages to mid grey instead of
aliasing, transparent edges keep their colour, and content stays aligned to within a pixel.

## Operations

| Operation | 1920×1080 | 3840×2160 | Allocated |
| --- | ---: | ---: | ---: |
| Crop | 0.04 ms | 0.41 ms | 128 B |
| Rotate 90 (lossless) | 2.0 ms | 20.2 ms | 96 B |
| Brightness (one pass) | 18.0 ms | 70.7 ms | 208 B |
| Four adjustments, fused | 47.4 ms | 188.9 ms | 4.8 KB |
| Four adjustments, unfused | 76.7 ms | 292.3 ms | 4.5 KB |
| Gaussian blur, radius 8 | 55.7 ms | 226.1 ms | 136 B |
| Unsharp mask | 103.8 ms | 331.0 ms | 240 B |
| Rotate 37° (resampled) | 222.0 ms | 888.6 ms | 312 B |

An arbitrary rotation costs far more than any other geometric operation, and almost all of that is the sampler,
which runs once per output pixel. Measured on a 12 megapixel image rotated by 37 degrees:

| `WarpSampling` | Throughput | Use when |
| --- | ---: | --- |
| `NearestNeighbor` | 39 MP/s | Pixel art, or a preview that will be re-rendered |
| `Bilinear` | 28 MP/s | Interactive rotation of a large image |
| `Bicubic` (default) | 8 MP/s | Final output |

A rotation by a multiple of 90 degrees never samples at all and runs at roughly 420 MP/s, so prefer
`OrientationOperation` whenever the angle allows it. The editor stays responsive at any setting because previews
render against a downscaled proxy; the full-resolution pass happens once, on export.

Two things to read from this:

- **Fusing adjustments is worth 1.55×.** Four adjustments in one pass beat four passes, because the cost is dominated by
  reading and writing pixels rather than by the arithmetic.
- **Rotating by a right angle is 100× cheaper than an arbitrary angle**, because it moves pixels rather than resampling
  them. The optimiser routes multiples of 90° to the lossless path automatically.

Blur is O(n) in the image and independent of radius: it is three box passes approximating a gaussian, so radius 40 costs
the same as radius 4.

## Pipelines

4000 × 3000 source.

| Pipeline | Optimised | Unoptimised | Gain |
| --- | ---: | ---: | ---: |
| Resize then crop | **47.9 ms** | 117.7 ms | 2.46× |
| Auto-orient, cap at 1920, sharpen | 231 ms | 213 ms | none |

The second row is the honest one: that pipeline has nothing to fuse, so the optimiser costs a little and saves nothing.
It is left on by default because the cases where it helps, help a lot, and the cases where it does not are within noise.

Resize followed by crop is the big win. Rendering only the surviving region is bit-identical to rendering everything and
cropping, because the filter kernels are still computed for the full output, so the discarded pixels are simply never
resampled.

Allocation for the whole pipeline is about 237 KB against a 48 MB image, which is the pooling working.

## Codecs

| Operation | 1024×768 | 3840×2160 | Allocated (4K) |
| --- | ---: | ---: | ---: |
| PNG decode | 10.5 ms | 104 ms | 37 MB |
| JPEG decode | 10.7 ms | 117 ms | 14 MB |
| JPEG encode, quality 85 | 17.0 ms | 175 ms | **6.7 MB** |
| PNG encode, level 1 | 25.1 ms | 252 ms | 98 MB |
| PNG encode, level 6 | 82.1 ms | 848 ms | 46 MB |

PNG compression level matters more than anything else here: level 6 is 3.4× slower than level 1 for a modest size gain.
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
| Stroke 100 arrows | 24.4 ms | 395 KB |
| Fill 100 ellipses | 32.5 ms | 596 KB |
| Fill 100 rectangles | 40.0 ms | 114 KB |
| Fill a 480 segment bezier path | 107 ms | 7.2 MB |

The rasteriser computes coverage one row at a time and never allocates a full-size coverage buffer, so drawing cost
scales with the shape's area rather than the canvas.

### The active edge table

The complex path case originally took **1075 ms**, because every sub-scanline walked every edge in the path: with five
sub-samples per row over 1080 rows and thousands of flattened segments, the cost was the product of the two rather than
the sum.

Sorting the edges by their top and maintaining an active set as the scanline advances:

| | Before | After |
| --- | ---: | ---: |
| 480 segment bezier path | 1075 ms | **107 ms** |
| Stroke 100 arrows | 34.2 ms | 24.4 ms |

10× on the complex case. This matters more than a synthetic benchmark suggests, because text is exactly this shape: a
line of glyphs is hundreds of small contours, and every text annotation pays it.

## Interactive editing

The editor edits through a proxy no larger than 2048px on its longest edge and replays the edit at full resolution on
export. For a 24 megapixel photo that is a 25× reduction in the pixels touched per preview, which is the difference
between a slider that tracks your finger and one that does not.

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
