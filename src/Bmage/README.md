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
> **Interactive Server** works too, with pixel data crossing the circuit. Prerendering, ASP.NET Core and console hosts
> get the headless API with the managed codecs.

Bmage's documentation website lives in [`Bit.Bmage.Demo`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bmage/Bit.Bmage.Demo/) - run the
`Bit.Bmage.Demo/Server` project, or visit [bmage.bitplatform.dev](https://bmage.bitplatform.dev).

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

## Packages

| Package | Use it when | Depends on |
| --- | --- | --- |
| `Bit.Bmage.Editor` | You want the `BmageEditor` component | `Bit.Bmage` |
| `Bit.Bmage` | You want browser codecs, loading and export, but your own UI | `Bit.Bmage.Core` |
| `Bit.Bmage.Core` | You want the engine with no Blazor dependency at all | nothing |

## Installation

```bash
dotnet add package Bit.Bmage          # browser integration: codecs, capabilities, loading, export
dotnet add package Bit.Bmage.Editor   # the editor component (pulls in Bit.Bmage)
dotnet add package Bit.Bmage.Core     # engine only, no Blazor dependency
```

Register the services in `Program.cs`:

```csharp
using Bit.Bmage;

builder.Services.AddBitBmageServices();
```

For server-side or console use, without any browser integration, register `AddBitBmageHeadlessServices()` instead.

Add the stylesheet if you use the editor:

```html
<link rel="stylesheet" href="_content/Bit.Bmage.Editor/bit-bmage-editor.css" />
```

In the browser, enable AOT compilation in the project that runs on WebAssembly. Pixel loops run 10 to 35× slower on the
default interpreter:

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

Crop, rotate, adjustments, filters, drawing, annotations, redaction, layers, history and export, in one component. It is
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
| Transforms | Crop, aspect crop, resize in six modes, rotate, flip, skew, affine and perspective warps |
| Adjustments | Brightness, contrast, saturation, hue, exposure, gamma, levels, temperature, tint, vibrance, shadows, highlights, opacity |
| Filters | Grayscale, sepia, invert, posterize, threshold, blur, sharpen, pixelate, noise, median denoise, vignette, custom convolution and colour matrices |
| Analysis | Histograms, luminance statistics, dominant colour palettes, auto-levels, auto-contrast, saliency-based smart cropping, low-quality placeholders |
| Drawing | Anti-aliased paths, strokes with caps, joins and dashes, gradients, ellipses, polygons, arrows, freehand, text |
| Annotations | Rectangle, ellipse, line, arrow, freehand, polygon, text, highlight, callout, numbered step, image stamp, redaction |
| Metadata | EXIF read and write, GPS, camera data, orientation, ICC profiles, XMP, three privacy policies |
| Pipelines | Composition, optimisation, lazy execution, cancellation, progress, batch processing |
| Editing | Non-destructive documents, undo and redo, memory-aware history, proxy previews |

## Performance

Every number below was measured with BenchmarkDotNet on desktop .NET 10 (see
[`Tests/Bit.Bmage.Benchmarks`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bmage/Tests/Bit.Bmage.Benchmarks/)). In the browser, with `RunAOTCompilation` enabled,
WebAssembly lands within 1.2 to 2.4× of these numbers; without it the default interpreter is 10 to 35× slower. See
[docs/performance.md](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/performance.md) for the full tables and how to reproduce them.

| Operation | Image | Time | Allocated |
| --- | --- | ---: | ---: |
| Thumbnail to 200px | 6000 × 4000 | 52 ms | 23 KB |
| Downscale to 1280px, Lanczos | 3840 × 2160 | 68 ms | 180 KB |
| Upload pipeline: orient, cap at 1920, sharpen | 4000 × 3000 | 231 ms | 237 KB |
| Four adjustments fused into one pass | 3840 × 2160 | 189 ms | 4.8 KB |
| JPEG encode, quality 85 | 3840 × 2160 | 175 ms | 6.7 MB |
| PNG decode | 3840 × 2160 | 104 ms | 37 MB |

## Where it runs

| Host | Support |
| --- | --- |
| Blazor WebAssembly | Everything |
| Blazor Web App, Interactive WebAssembly | Everything |
| Blazor Web App, Interactive Auto | Everything once interactive |
| Blazor Web App, Interactive Server | Everything, with pixel data crossing the circuit; see [docs/browser-support.md](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/browser-support.md) |
| Prerendering | Managed codecs only, and capability detection reports `Prerendering`; call browser features from `OnAfterRenderAsync` |
| ASP.NET Core, console, worker | Headless API with the managed codecs, no browser involved |

## MCP server (for AI agents)

The demo website hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bmage.bitplatform.dev/mcp`, and it is also served from the bit platform's combined endpoint at
`https://bitplatform.dev/mcp`. Point your coding agent at either to let it search the Bmage API, read worked examples
and setup guides, and review Bmage code. See the MCP server page of the demo website for the full tool list.

## Tests

```bash
cd src/Bmage/Tests/Bit.Bmage.Tests && dotnet test        # engine tests, no browser needed
cd src/Bmage/Tests/Bit.Bmage.Tests.E2E && dotnet test    # drives a real browser against the demo website
cd src/Bmage/Tests/Bit.Bmage.Tests.Mcp && dotnet test    # the MCP server, hosted in-memory
dotnet run --project src/Bmage/Tests/Bit.Bmage.Benchmarks -c Release -- --filter "*"
```

## Documentation

- [Getting started](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/getting-started.md)
- [Architecture](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/architecture.md)
- [Pipelines](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/pipelines.md)
- [The editor component](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/editor.md)
- [Headless processing](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/headless.md)
- [Formats and capabilities](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/formats.md)
- [Metadata and privacy](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/metadata-and-privacy.md)
- [Performance](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/performance.md)
- [Security](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/security.md)
- [Browser support](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/browser-support.md)
- [Extending](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/extending.md)
- [Troubleshooting](https://github.com/bitfoundation/bitplatform/blob/develop/src/Bmage/docs/troubleshooting.md)

## License

MIT. See [LICENSE](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE).
