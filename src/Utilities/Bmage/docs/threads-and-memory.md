# Threads, workers and memory

Bmage spreads an operation over as many cores as it is told to, keeps the browser's page thread free for the page, and
lets a caller predict and bound the memory a piece of work takes. None of it needs configuring; every part of it can be.

## Every core, when asked

Operations whose rows are independent split them into bands and run them on `MaxDegreeOfParallelism` threads:
resampling and the box prepass behind large reductions, rotations, skews and perspective warps, per-pixel adjustments,
convolution, median, vignette, lossless orientation, masks, layer compositing and drawing images. Each band brings a few
rows of scratch of its own and shares read-only state such as filter kernels and lookup tables, so the pixels are
identical however many threads run; a test asserts that for every one of them. The default is one thread, the one that
runs the operation, and on one thread the code is the code it always was: an A/B run against the previous release put
every operation below within 2% of it.

Measured with BenchmarkDotNet on an Intel Core i9-10900 (10 cores, 20 threads), .NET 10.0.13, Windows 10, two warm-ups
and six iterations:

| Operation | 1 thread | 20 threads | Speed-up |
| --- | ---: | ---: | ---: |
| Downscale 6000 × 4000 to 1280, Lanczos3 | 251 ms | **54 ms** | 4.7× |
| Rotate 3840 × 2160 by 37°, bicubic | 450 ms | **74 ms** | 6.1× |
| Saturation and hue, 3840 × 2160 | 95 ms | **15 ms** | 6.3× |
| Median, radius 1, 1920 × 1080 | 132 ms | **16 ms** | 8.5× |
| Exposure through a gradient mask, 3840 × 2160 | 40 ms | **7.6 ms** | 5.2× |
| Two masked layers composited, 3840 × 2160 | 487 ms | **83 ms** | 5.9× |

Reproduce them with `--filter "*ParallelBenchmarks*"`. Blurs and sharpening stay on one thread: they stream rows through
running sums, and splitting them would change the sums and so the pixels.

### Choosing the number of threads

| Where | Set | Why |
| --- | --- | --- |
| Server | Nothing: one | Requests, batch items and editor circuits already run side by side. Each of them fanning out over every core would multiply the threads they compete for, and hold a thread-pool thread waiting on every run. |
| Desktop, MAUI, a command-line tool or background job | `Environment.ProcessorCount` | One image at a time finishes sooner. |
| Browser | Nothing: one | Blazor WebAssembly runs .NET on the page's thread, and work handed to another thread could never be waited for there. |

```csharp
// For every operation the services run: processors, loaders and editors.
builder.Services.AddBitBmageServices(options => options.MaxDegreeOfParallelism = Environment.ProcessorCount);

// For one run:
pipeline.Execute(image, new PipelineExecutionOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
```

In a batch every image in flight uses that many threads, so `MaxConcurrency` times `MaxDegreeOfParallelism` is what a
batch can occupy. A WebAssembly app built with `<WasmEnableThreads>true</WasmEnableThreads>` can raise the browser's
default the same way. Whatever the number, progress is reported on the thread that called the operation, so a sink that
updates a UI or a Blazor component needs no marshalling of its own.

A custom `PointOperation` runs on one thread unless it overrides `CanProcessRowsConcurrently` to say its `ProcessRow`
keeps no shared state; a fused run is only as concurrent as its least safe stage. A custom `ImageOperation` is its own
code and chooses for itself.

## Web Workers

In the browser, decoding, encoding and resizing the editor's working copy run in a small pool of Web Workers
(`_content/Bit.Bmage.Blazor/bit-bmage-worker.js`) wherever the browser can do them there, which is everywhere with a 2D
`OffscreenCanvas`: current Chrome, Edge, Firefox, and Safari from 16.4. The work is the browser's own either way; what
changes is where it runs. The page's thread, which on WebAssembly is also .NET's, keeps painting and handling input while
a photo decodes or encodes, and several decodes or encodes at once, as a batch makes, spread over several workers and
so over several cores.

- **The pool** starts on first use with one worker and grows, while every worker is busy, to one per spare core and at
  most four. A new worker answers a probe before it is given any work, each worker runs one piece of work at a time,
  and one that stops takes only its own piece with it.
- **Nothing is copied.** Files and pixels are transferred to a worker and back. A buffer a caller hands the module's
  `decode` or `encode` is copied first, so the caller's array is never left detached.
- **Nothing breaks.** Without `Worker` or `OffscreenCanvas`, when a Content Security Policy blocks the worker, or when
  a worker fails at a piece of work, that work runs on the page's thread exactly as before. A failed piece of work hands
  its input back with the error, so no copy is kept aside for the fallback. A format the browser cannot write fails the
  same way in both places and is reported, never retried.
- **It can be turned off**, or capped, with `BitBmageOptions.MaxCodecWorkers`; 0 keeps everything on the page.

A Content Security Policy that lists sources explicitly needs `worker-src 'self'`, or a `script-src 'self'` it falls
back to. Blob URLs are never used for workers.

## Memory

### Predicting it

```csharp
var estimate = pipeline.EstimateMemory(new Size(6000, 4000));
estimate.PeakBytes;     // the most pixel memory the run holds at once, its result included, the source not
estimate.OutputBytes;   // the result
```

The estimate follows the buffers each step holds: the image it receives, the one it produces, and the working copies
the heavier operations take, such as a mask or a copy of a region for a median. A test holds it between what a run
really allocates and twice that.

### Bounding it

A `BudgetedPixelAllocator` hands out memory from another allocator while what it has outstanding stays within its
budget, and refuses past that with `ImageMemoryBudgetExceededException` before anything is allocated. Every buffer and
mask an operation makes comes from the allocator in its context, so one budget bounds a request, a session or a batch,
on any number of threads:

```csharp
var budget = new BudgetedPixelAllocator(256L * 1024 * 1024);
using var result = pipeline.Execute(source, new PipelineExecutionOptions { Allocator = budget });
Console.WriteLine($"{budget.PeakBytes / 1048576.0:0.0} MB at the peak");
```

Small scratch, a row or a lookup table, comes from the shared array pool and is not counted: kilobytes against
megabytes.

### Batches of any length

```csharp
var items = paths.Select(path => BatchItem.Deferred(async ct => (ReadOnlyMemory<byte>)await File.ReadAllBytesAsync(path, ct), path));

await foreach (var result in processor.ProcessBatchStreamAsync(items, pipeline, new BatchOptions
{
    MaxConcurrency = 4,
    MaxMemoryBytes = 768L * 1024 * 1024,
}))
{
    // Save the result and let it go; the batch keeps nothing.
}
```

- **Deferred items** read their file only when the batch reaches them, so a batch of a thousand photos never holds a
  thousand files.
- **Streaming** hands each result over as it completes, in completion order, so outputs do not pile up either. The
  source may be an `IAsyncEnumerable<BatchItem>`, such as files arriving from an upload.
- **`MaxMemoryBytes`** admits images by their expected peak, estimated from each file's header before decoding: the
  file, its pixels, the pipeline's working buffers and an allowance for encoding. Small photos then run several at a
  time and a large one runs with room to spare. Requests are admitted in order, so a large image is never starved by a
  stream of small ones, and one larger than the whole budget runs alone rather than never.

It schedules by estimate; for a hard ceiling as well, give the processor a `BudgetedPixelAllocator`.

### What masks and layers cost

A selective edit costs the edit over the rows its mask can reach, plus rendering the mask, one byte per pixel. The
exposure change through a gradient covering the top half of a 4K frame, above, takes 40 ms on one thread, against 9 ms
for the same exposure over the whole frame as a lookup table; most of the difference is drawing the gradient and mixing.
Feathering is three box passes over the byte plane. A blend mode other than `SourceOver` evaluates the W3C formula per
pixel in floating point, which is why two masked 4K layers take about half a second on one thread, and 83 ms on twenty.
