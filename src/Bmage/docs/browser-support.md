# Browser support and hosting models

## Hosting models

| Model | What works | What to know |
| --- | --- | --- |
| Blazor WebAssembly | Everything | The intended home. Processing runs on the user's device with no network involved. |
| Blazor Web App, `InteractiveWebAssembly` | Everything | Same as above once the component is interactive. |
| Blazor Web App, `InteractiveAuto` | Everything once interactive | The first visit runs on the server while the runtime downloads; see the Server row. |
| Blazor Web App, `InteractiveServer` | Everything | Works, but pixels cross the circuit. See below. |
| Prerendering | Managed codecs only | No browser exists yet. Call browser features from `OnAfterRenderAsync`. |
| ASP.NET Core, console, worker | Headless API | `AddBitBmageHeadlessServices`. Managed codecs only, no JavaScript. |

### Interactive Server

Everything works, because JavaScript interop works over the circuit. What changes is where the bytes travel: the browser
decodes an image and the pixels are streamed to the server for processing, then the result is streamed back to be
encoded.

For a 4000 × 3000 photo that is 48 MB in each direction. On a local network this is unremarkable; over a poor mobile
connection it is not.

If you are targeting Interactive Server and images are central to your application:

- Constrain the working size aggressively: `options.MaxInteractiveDimension = 1200`.
- Use `DecodeOptions.MaxSize` so the browser downscales before the pixels are sent at all.
- Prefer the headless API on the server for bulk work, and use the browser only for what needs a browser.
- Consider whether the client project with WebAssembly is a better fit for the image-heavy pages.

The library does not pretend a browser API works where it does not: `IImageCapabilityProvider` reports
`ImageEnvironment.BrowserServer` so you can adapt.

### Prerendering

During prerendering there is no browser, so `IJSRuntime` cannot be used. Rather than throwing,
`IImageCapabilityProvider.GetAsync` returns the managed capability set with `Environment` set to `Prerendering`, so a
prerendered page renders something sensible and detects properly once interactive.

Move browser work to `OnAfterRenderAsync`:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    _capabilities = await Capabilities.GetAsync();
    StateHasChanged();
}
```

If you call a browser feature too early anyway, you get an `ImageCapabilityException` that says so explicitly rather
than an opaque interop error.

## Browsers

| Browser | Decode | Encode | Notes |
| --- | --- | --- | --- |
| Chrome, Edge 90+ | PNG, JPEG, WebP, AVIF, GIF, BMP | PNG, JPEG, WebP | AVIF encoding is generally unavailable |
| Firefox 93+ | PNG, JPEG, WebP, AVIF, GIF, BMP | PNG, JPEG, WebP | |
| Safari 16+ | PNG, JPEG, WebP, AVIF, GIF, BMP | PNG, JPEG, WebP | Tighter canvas and memory limits, especially on iOS |
| Safari 14 to 15 | PNG, JPEG, WebP, GIF, BMP | PNG, JPEG | No AVIF |

Do not rely on this table. It is a summary of a moving target, and the library probes the actual browser at runtime for
exactly that reason. See [formats.md](formats.md).

### Safari and iOS

Safari is the constraint worth designing around:

- **Canvas limits are much lower.** Older iPhones cap around 4096px per side, against 32767 on desktop Chrome. Exceeding
  the limit produces a silently blank canvas, which is why `MaxCanvasDimension` is probed rather than assumed.
- **Memory pressure kills tabs.** iOS reclaims aggressively. Keep `MaxInteractiveDimension` modest and batch concurrency
  at 1 or 2.
- **`OffscreenCanvas` arrived late.** The library falls back to a detached canvas element automatically.
- **Clipboard reads need a user gesture** and often a permission prompt.

`ImageCapabilities.SuggestedMaxWorkingDimension` derives a cap from the reported device memory and canvas limit, which
is a reasonable default for interactive work.

## Feature availability

Check rather than assume. Each of these is a real capability with a real fallback:

```csharp
var c = await capabilities.GetAsync();

if (c.SupportsClipboardRead)  { /* offer a paste button */ }
if (c.SupportsClipboardWrite) { /* offer copy to clipboard, PNG only */ }
if (c.SupportsFileSystemAccess) { /* a real save dialog */ } else { /* a download */ }
if (c.SupportsCamera) { /* offer capture */ }
```

`ImageExporter.SaveAsAsync` already does the File System Access fallback for you: it uses the save dialog where
available and a download where not, and returns false when the user cancels.

## WebAssembly runtime features

`SupportsWasmSimd` reports whether the runtime has SIMD, which .NET uses for `Vector4` arithmetic in the colour pipeline.
`SupportsWasmThreads` reports multithreading, which is off by default in Blazor. Neither is required; they inform how
much work is reasonable per frame.

## What is not used yet

**Web Workers and OffscreenCanvas.** Both are detected and reported, and the architecture does not preclude them, but
the benchmark evidence says the wins for the common operations are smaller than the complexity of marshalling pixels to a
worker. The brief for this library was to benchmark before adding worker complexity, and that is what the numbers said.

**WebCodecs.** Detected and reported. It offers little over `createImageBitmap` for still images, and its value is in
video.

## Testing your own integration

The browser test project starts the demo application and drives a real Chrome against it:

```bash
cd src && dotnet test --project Bmage/Tests/Bit.Bmage.Tests.E2E/Bit.Bmage.Tests.E2E.csproj
```

It uses Playwright's bundled browser when installed, falls back to a system Chrome or Edge, and skips with a clear
message when neither is available, so a checkout without browsers still builds and runs the rest of the suite.
