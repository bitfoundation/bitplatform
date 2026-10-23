# bit Bmage

Browser-side image processing for Blazor. Load, transform, annotate and export images entirely on the user's device,
from strongly typed C#.

No server round trips. No base64. No JavaScript image library.

```csharp
var result = await processor.ProcessAsync(
    uploadedFile,
    ImagePipeline.Create()
        .AutoOrient()
        .Cover(512, 512)
        .Sharpen(0.3f),
    new ImageExportOptions
    {
        Format      = ImageFormat.WebP,
        Quality     = 0.82,
        Metadata    = MetadataPolicy.StripSensitive,
        MaxFileSize = 120 * 1024,
    });
```

That runs in the browser. The original file never leaves it, the phone's rotation is applied, the GPS coordinates are
stripped, and the result is a WebP that fits the size budget you asked for.

> Targets **.NET 8, 9, and 10** · Everything works on **Blazor WebAssembly** and **Interactive Auto** once interactive.
> **Interactive Server** works too: PNG, JPEG, BMP and GIF are coded on the server, while WebP, AVIF and editor previews
> cross the circuit. Prerendering, ASP.NET Core and console hosts get the headless API with the managed codecs.

Bmage's documentation website lives in [`Demo`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bmage/Demo/) - run the
`Demo/Server` project, or visit [bmage.bitplatform.dev](https://bmage.bitplatform.dev).

---

## Table of Contents

- [Why Bmage](#why-bmage)
- [Packages](#packages)
- [Installation](#installation)
- [The simple path](#the-simple-path)
- [The full editor](#the-full-editor)
- [What is in the box](#what-is-in-the-box)
- [Performance](#performance)
- [Where it runs](#where-it-runs)
- [MCP server (for AI agents)](#mcp-server-for-ai-agents)
- [Tests](#tests)
- [Documentation](#documentation)
- [License](#license)

---

## Why Bmage

Blazor developers who need to crop an avatar or shrink an upload have had two options: send the full-resolution file to
a server, or assemble several JavaScript libraries behind hand-written interop. Bmage is a third option: one coherent,
Blazor-native image processing platform where the engine itself is C#.

- **Non-destructive by construction.** An edit is a list of operations, not a mutated buffer. Undo costs a few hundred
  bytes, previews are cheap, and any earlier step can be changed without redoing the rest.
- **The engine is this project.** The resampler, rasteriser, colour pipeline, TrueType text engine, and the PNG, JPEG,
  BMP and GIF codecs are implemented here. The browser is used only for what only it can do: WebP and AVIF.
- **Large images stay usable.** Interactive editing runs on a proxy sized for the display and replays at full resolution
  on export, so a 24 megapixel photo does not stall the interface.
- **Privacy is the default.** Metadata is removed unless you ask for it. Location data cannot leak into an exported
  avatar by accident.
- **Nothing about the browser is assumed.** Format support is probed by actually encoding a pixel. When a browser
  substitutes PNG for the AVIF you requested, you get an exception, not a silent surprise.
- **Headless first.** Every capability works without the editor component, so the same pipeline runs in an upload
  handler or a console tool.
- **Selective edits and layers.** Any edit can be applied through a mask: a gradient, a radial spotlight, a tone or
  colour range, brush strokes, a segmentation model's matte. Image, fill and adjustment layers composite with the W3C
  blend modes.
- **Edits are data.** A document, pipeline, layers, masks and annotations included, writes to JSON and reads back as the
  same edit, so the browser can send the edit instead of the pixels and a server can replay it at full resolution.
- **Every core, when you want it.** Set `MaxDegreeOfParallelism` and operations spread their rows over that many cores
  with identical pixels; one thread by default, which suits a busy server. In the browser decoding and encoding run in
  Web Workers, off the page's thread.
- **Memory you can predict.** A pipeline estimates its peak before it runs, a budgeted allocator enforces a ceiling,
  and a batch streams its files and results so it holds only the images in flight.

## Packages

| Package | What it contains | Depends on |
| --- | --- | --- |
| `Bit.Bmage` | The engine, browser codecs, capabilities, loading and export, headless processing on a server, and the `BmageEditor` / `BmageDropZone` components | `Microsoft.AspNetCore.Components.Web` |

Everything ships in the one package. The editor's stylesheet is opt-in, so an app that brings its own UI or only
processes images headlessly never loads it.

## Installation

```bash
dotnet add package Bit.Bmage
```

Register the services in `Program.cs`:

```csharp
using Bit.Bmage;

builder.Services.AddBitBmageServices();
```

For server-side or console use, without any browser integration, register `AddBitBmageHeadlessServices()` instead.

A WebAssembly app whose only use of Bmage is `BmageEditor` can skip the registration and lazy load `Bit.Bmage.wasm`
instead; see [lazy loading](docs/getting-started.md#lazy-loading-bmage-on-webassembly).

Add the stylesheet if you use the editor:

```html
<link rel="stylesheet" href="_content/Bit.Bmage/bit-bmage-editor.css" />
```

In the browser, consider AOT compilation in the project that runs on WebAssembly. Bmage is tuned for the default
interpreter, where the editor opens a 12 megapixel photo in about 50 ms, but work that stays in .NET still runs 4 to 12×
faster compiled ahead of time:

```xml
<RunAOTCompilation>true</RunAOTCompilation>
```

## The simple path

```razor
@inject ImageLoader Loader
@inject ImageExporter Exporter

<InputFile OnChange="OnChange" accept="image/*" />

@code {
    async Task OnChange(InputFileChangeEventArgs e)
    {
        using var image = await Loader.LoadAsync(e.File);
        using var thumb = await ImagePipeline.Create()
            .AutoOrient()
            .MaxSize(400, 400)
            .ExecuteAsync(image);

        await Exporter.DownloadAsync(thumb, "thumbnail",
            new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.85 });
    }
}
```

## The full editor

```razor
@using Bit.Bmage.Editor.Components

<BmageEditor @ref="editor" />
<button @onclick="() => editor.DownloadAsync(&quot;edited&quot;)">Save</button>
```

Crop, rotate, straighten, resize, canvas size, trim, adjustments with a histogram, a colour mixer, filters, painting with
a brush and an eraser, fill, magic eraser, eyedropper, rectangle, ellipse, lasso and magic wand selections,
annotations, redaction, layers, before and after comparison, history, project files and export, in one component. It is
themeable through CSS custom properties, fully localisable, keyboard accessible, touch friendly and right-to-left aware.
Every part of its UI can be replaced while keeping the engine:

```razor
<BmageEditor Strings="german" Direction="rtl" Class="bmg-theme-dark">
    <Toolbar Context="editor">
        <button @onclick="() => editor.SetTool(EditorToolKind.Crop)">Crop</button>
        <button disabled="@(!editor.CanUndo)" @onclick="editor.Undo">Undo</button>
    </Toolbar>
</BmageEditor>
```

## What is in the box

| Area | What you get |
| --- | --- |
| Loading | `IBrowserFile`, byte arrays, streams, data URLs, object URLs, remote URLs, clipboard, drag and drop, camera |
| Decoding | PNG, JPEG, BMP and GIF in managed code; PNG, JPEG, WebP, AVIF and GIF through the browser |
| Encoding | PNG, JPEG and BMP in managed code; PNG, JPEG, WebP and AVIF through the browser |
| Transforms | Crop, aspect crop, resize in six modes, rotate, flip, straighten, canvas size and margins, trim to content, skew, affine and perspective warps; annotations and layers stay on the content they mark |
| Adjustments | Brightness, contrast, saturation, hue, exposure, gamma, levels, temperature, tint, vibrance, shadows, highlights, whites, blacks, white balance (picked or automatic), per-colour HSL, channel mixer, colorize, opacity |
| Filters | Grayscale, black and white mixer, sepia, invert, posterize, threshold, gradient map and duotone, blur, motion blur, sharpen, high pass, pixelate, noise, median denoise, vignette, custom convolution and colour matrices |
| Analysis | Histograms, luminance statistics, dominant colour palettes, auto-levels, auto-contrast, saliency-based smart cropping, content bounds for trimming, colour sampling, low-quality placeholders |
| Masks | Rectangles, ellipses, lassos, paths, brush strokes, linear and radial gradients, luminance and colour ranges, magic wand, bitmap mattes, combinations; expand, contract, border, feathering, inversion, relative units; masked edits, alpha cut-outs and plain background removal |
| Layers | Image, fill, adjustment and paint layers (brush with hardness and pen pressure, eraser) with opacity, masks and the W3C blend modes, from `Multiply` and `Screen` to `Color` and `Luminosity`; pass-through and isolated groups, and clipping to the layer below |
| Drawing | Anti-aliased paths, strokes with caps, joins and dashes, gradients, ellipses, polygons, arrows, freehand, text |
| Annotations | Rectangle, ellipse, line, arrow, freehand, polygon, text, highlight, callout, numbered step, image stamp, redaction |
| Metadata | EXIF read and write, GPS, camera data, orientation, ICC profiles, XMP, three privacy policies |
| Pipelines | Composition, optimisation, lazy execution, cancellation, progress, memory estimates, batch processing with streamed inputs and outputs and memory-based admission |
| Editing | Non-destructive documents with layers, undo and redo, memory-aware history, proxy previews, JSON save and restore, self-contained project files that reopen editable |
| Concurrency | Row-parallel operations with identical pixels on any number of threads; browser codecs in a Web Worker pool |

## Performance

Every number below was measured with BenchmarkDotNet on desktop .NET 10 (see
[`Tests/Bit.Bmage.Benchmarks`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bmage/Tests/Bit.Bmage.Benchmarks/)). In the browser, with `RunAOTCompilation` enabled,
WebAssembly lands within about 2.5× of these numbers, and the default interpreter is 4 to 12× slower than that; the WebAssembly
table in performance.md has both. See
[docs/performance.md](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/performance.md) for the full tables and how to reproduce them.

| Operation | Image | Time | Allocated |
| --- | --- | ---: | ---: |
| Thumbnail to 200px | 6000 × 4000 | 23 ms | 23 KB |
| Downscale to 1280px, Lanczos | 3840 × 2160 | 40 ms | 180 KB |
| Upload pipeline: orient, cap at 1920, sharpen | 4000 × 3000 | 101 ms | 238 KB |
| Four adjustments fused into one pass | 3840 × 2160 | 67 ms | 4.6 KB |
| JPEG encode, quality 85 | 3840 × 2160 | 122 ms | 6.7 MB |
| PNG decode | 3840 × 2160 | 88 ms | 37 MB |

Those are on one thread, the default. Given more with `MaxDegreeOfParallelism`, operations spread their rows over every
core with identical pixels: on a 10 core machine a 24 megapixel downscale runs 4.7× faster, a 4K rotation 6.1×, a
median filter 8.5×. In the browser, decoding and encoding run in Web Workers, off the page's thread.

## Where it runs

| Host | Support |
| --- | --- |
| Blazor WebAssembly | Everything |
| Blazor Web App, Interactive WebAssembly | Everything |
| Blazor Web App, Interactive Auto | Everything once interactive |
| Blazor Web App, Interactive Server | Everything; PNG, JPEG, BMP and GIF are coded on the server, WebP, AVIF and editor previews cross the circuit; see [docs/browser-support.md](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/browser-support.md) |
| Prerendering | Managed codecs only, and capability detection reports `Prerendering`; call browser features from `OnAfterRenderAsync` |
| ASP.NET Core, console, worker | Headless API with the managed codecs, no browser involved |

## MCP server (for AI agents)

The demo website hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bmage.bitplatform.dev/mcp`. Point your coding agent at it to let it search the Bmage API, read worked examples
and setup guides, and review Bmage code. See the MCP server page of the demo website for the full tool list.

## Tests

```bash
cd src/Utilities/Bmage/Tests/Bit.Bmage.Tests && dotnet test        # engine tests, no browser needed
cd src/Utilities/Bmage/Tests/Bit.Bmage.Tests.E2E && dotnet test    # drives a real browser against the demo website
cd src/Utilities/Bmage/Tests/Bit.Bmage.Tests.Mcp && dotnet test    # the MCP server, hosted in-memory
dotnet run --project src/Utilities/Bmage/Tests/Bit.Bmage.Benchmarks -c Release -- --filter "*"
```

## Documentation

- [Getting started](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/getting-started.md)
- [Architecture](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/architecture.md)
- [Pipelines](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/pipelines.md)
- [Layers, masks and blend modes](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/layers-and-masks.md)
- [The editor component](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/editor.md)
- [Headless processing](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/headless.md)
- [Formats and capabilities](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/formats.md)
- [Metadata and privacy](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/metadata-and-privacy.md)
- [Performance](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/performance.md)
- [Threads, workers and memory](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/threads-and-memory.md)
- [Security](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/security.md)
- [Browser support](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/browser-support.md)
- [Extending](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/extending.md)
- [Troubleshooting](https://github.com/bitfoundation/bitplatform/blob/develop/src/Utilities/Bmage/docs/troubleshooting.md)

## License

MIT. See [LICENSE](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE).
