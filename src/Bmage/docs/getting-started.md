# Getting started

## Install

Pick the package that matches what you need. Each one includes the ones below it.

| Package | Use it when | Depends on |
| --- | --- | --- |
| `Bit.Bmage.Editor` | You want the editor component | `Bit.Bmage` |
| `Bit.Bmage` | You want browser codecs, loading and export, but your own UI | `Bit.Bmage.Core` |
| `Bit.Bmage.Core` | You want the engine with no Blazor dependency at all | nothing |

```bash
dotnet add package Bit.Bmage.Editor
```

## Register the services

In `Program.cs` of the project that runs in the browser. For a Blazor Web App using WebAssembly or Auto render modes,
that is the client project. Register it in the server project as well whenever the server instantiates the same
components: during prerendering (on by default) and for any component that uses Interactive Server.

```csharp
using Bit.Bmage;   // AddBitBmageServices and AddBitBmageHeadlessServices live here

builder.Services.AddBitBmageServices();
```

This registers `ImageLoader`, `ImageExporter`, `IImageCapabilityProvider` and `ImageProcessorFactory`. It does not
register `ImageProcessor` directly, because in a browser the processor can only be built once the browser's codec
support has been probed, and that probe is asynchronous. Get one from the factory when you need the lower level API:

```csharp
@inject ImageProcessorFactory ProcessorFactory

// Inside an event handler or OnAfterRenderAsync, never during prerendering.
var processor = await ProcessorFactory.GetAsync();
```

`AddBitBmageHeadlessServices` has no such constraint and does register `ImageProcessor` directly, because the managed
codecs need no probing. That is the one difference in the service graph between the two registrations.

With options:

```csharp
builder.Services.AddBitBmageServices(options =>
{
    // Refuse anything larger than this before decoding, to protect against decompression bombs.
    options.Limits = ImageLimits.Default with { MaxPixels = 40_000_000 };

    // Largest upload accepted from a file input.
    options.MaxSourceBytes = 32 * 1024 * 1024;

    // Longest edge used for interactive editing; larger images edit through a proxy.
    options.MaxInteractiveDimension = 2048;
});
```

Services are registered as scoped. In WebAssembly that scope lasts for the whole application; in Server there is one
scope per circuit, so browser state is never shared between users.

## Add the stylesheet

Only needed for `Bit.Bmage.Editor`. Put it in `index.html` or `App.razor`:

```html
<link rel="stylesheet" href="_content/Bit.Bmage.Editor/bit-bmage-editor.css" />
```

The browser bridge script is loaded on demand by the library. You do not add a script tag for it.

## Enable AOT compilation

For WebAssembly, add this to the project that runs in the browser:

```xml
<PropertyGroup>
  <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

All image processing is C#, and on Blazor's default interpreter it runs 10 to 35× slower than compiled code. AOT needs
the `wasm-tools` workload and only takes effect on `dotnet publish`, so a page that feels slow under `dotnet run` may be
fine once published. [performance.md](performance.md#webassembly-compile-ahead-of-time) has the measurements and the
trade-offs.

## Your first image

```razor
@page "/upload"
@inject ImageLoader Loader
@inject ImageExporter Exporter

<InputFile OnChange="OnChange" accept="image/*" />

@if (_preview is not null)
{
    <img src="@_preview" alt="Processed preview" />
    <p>@_size bytes</p>
}

@code {
    private string? _preview;
    private long _size;

    private async Task OnChange(InputFileChangeEventArgs e)
    {
        // Decode with the browser's codecs, honouring the phone's EXIF rotation.
        using var image = await Loader.LoadAsync(e.File);

        // Process on the decoded pixels.
        using var processed = await ImagePipeline.Create()
            .MaxSize(800, 800)
            .Sharpen(0.3f)
            .ExecuteAsync(image);

        // Encode, preferring WebP but falling back where it is unavailable.
        var encoded = await Exporter.EncodeWithFallbackAsync(
            processed,
            [ImageFormat.WebP, ImageFormat.Jpeg],
            new ImageExportOptions { Quality = 0.85 });

        _size = encoded.Length;
        _preview = await Exporter.CreateObjectUrlAsync(encoded);
    }
}
```

Two things in that snippet matter and are easy to miss.

**Dispose your images.** An `ImageBuffer` holds pooled memory: a 4000 × 3000 image is 48 MB. `using` returns it to the
pool immediately instead of waiting for a collection. Buffers you receive from `LoadAsync`, `ExecuteAsync` and
`RenderFullAsync` are yours to dispose. Buffers exposed as properties, such as `ImageEditSession.Preview`, are not.

**Prefer object URLs to data URLs.** `CreateObjectUrlAsync` hands the browser the bytes directly. `ToDataUrl()` exists
but base64 inflates the payload by a third and forces it through the DOM as a string. Release object URLs with
`ReleaseObjectUrlAsync`, or dispose the exporter, which releases every URL it created.

## The full editor instead

```razor
@page "/edit"

<div style="height: 70vh">
    <BmageEditor @ref="_editor" OnExported="OnExported" />
</div>

<button @onclick="Save">Save</button>

@code {
    private BmageEditor? _editor;

    private Task Save() => _editor!.DownloadAsync("edited",
        new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.9 });

    private void OnExported(EncodedImage image)
    {
        // Also called when the user exports from the editor's own panel.
    }
}
```

The editor needs a height. It fills its container, and a container with no height collapses to nothing.

## Without a browser

The same engine runs on a server or in a console application with no JavaScript at all. Only the managed codecs are
available there: PNG, JPEG, BMP and GIF for decoding, PNG, JPEG and BMP for encoding.

```csharp
using Bit.Bmage;

builder.Services.AddBitBmageHeadlessServices();
```

```csharp
app.MapPost("/upload", async (IFormFile file, ImageProcessor processor) =>
{
    using var buffer = new MemoryStream();
    await file.OpenReadStream().CopyToAsync(buffer);

    var thumbnail = await processor.ProcessAsync(
        buffer.ToArray(),
        ImagePipeline.Create().AutoOrient().MaxSize(400, 400),
        new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.8 });

    return Results.File(thumbnail.Data, thumbnail.MimeType);
});
```

## When things run

Browser features need a live browser. During prerendering there is not one yet, so calling them from
`OnInitializedAsync` on the server will fail. Call them from `OnAfterRenderAsync` or from an event handler:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    _capabilities = await Capabilities.GetAsync();
    StateHasChanged();
}
```

`IImageCapabilityProvider.GetAsync` is deliberately forgiving here: when no browser is reachable it returns the managed
capability set with `Environment` set to `Prerendering` rather than throwing, so a prerendered page still renders
something sensible and re-detects once interactive.

## Next

- [Pipelines](pipelines.md) covers composing and optimising operations.
- [The editor component](editor.md) covers theming, localisation and replacing parts of the UI.
- [Formats and capabilities](formats.md) covers WebP, AVIF and what to do when a browser cannot write them.
- [Metadata and privacy](metadata-and-privacy.md) covers EXIF, GPS and the three metadata policies.
