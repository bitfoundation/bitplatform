# Browser support and hosting models

## Hosting models

| Model | What works | What to know |
| --- | --- | --- |
| Blazor WebAssembly | Everything | The intended home. Processing runs on the user's device with no network involved. |
| Blazor Web App, `InteractiveWebAssembly` | Everything | Same as above once the component is interactive. |
| Blazor Web App, `InteractiveAuto` | Everything once interactive | The first visit runs on the server while the runtime downloads; see the Server row. |
| Blazor Web App, `InteractiveServer` | Everything | Works, but pixels cross the circuit. See below. |
| Blazor Hybrid (MAUI, WPF, Windows Forms) | Everything | .NET runs in the app and reaches the WebView over its interop bridge; no network involved. See below. |
| Prerendering | Managed codecs only | No browser exists yet. Call browser features from `OnAfterRenderAsync`. |
| ASP.NET Core, console, worker | Headless API | `AddBitBmageHeadlessServices`. Managed codecs only, no JavaScript. |

### Interactive Server

Everything works, because JavaScript interop works over the circuit. What changes is where the bytes travel. PNG, JPEG,
BMP and WebP are decoded and encoded on the server by the managed codecs, and GIF and AVIF decoded, so only the file
crosses the circuit. Encoding AVIF needs the browser, and so does decoding a photo whose colours need colour management
(a non-sRGB colour profile, or a wide gamut or HDR AVIF): its pixels are streamed between the browser and the server,
which for a 4000 × 3000 photo is 48 MB. The editor's preview also has to be sent to the browser each time it changes, 12.6 MB
for a 2048 pixel proxy.

On a local network this is unremarkable; over a poor mobile connection it is not.

If you are targeting Interactive Server and images are central to your application:

- Constrain the working size aggressively: `options.MaxInteractiveDimension = 1200`.
- Use `DecodeOptions.MaxSize` so the browser downscales before the pixels are sent at all.
- Prefer the headless API on the server for bulk work, and use the browser only for what needs a browser.
- Consider whether the client project with WebAssembly is a better fit for the image-heavy pages.

The library does not pretend a browser API works where it does not: `IImageCapabilityProvider` reports
`ImageEnvironment.BrowserServer` so you can adapt.

### Blazor Hybrid (MAUI, WPF, Windows Forms)

Everything works. The components run in compiled .NET inside the app's own process and render into a WebView, which
Bmage reaches over JavaScript interop - the same position as Interactive Server, so `IImageCapabilityProvider` reports
`ImageEnvironment.BrowserServer` and the codecs are chosen the same way. PNG, JPEG, BMP and WebP are decoded and
encoded by the managed codecs in the app, and GIF and AVIF decoded; AVIF exports and photos whose colours need colour management go
through the WebView.
Nothing crosses a network, but those pixels, and the editor's previews, are copied through the WebView's message
channel, so the managed formats are the cheaper ones here.

Setting it up differs from a Web App in three places:

- **One container, and the WebView is there from the first render.** Register the services once, next to the
  WebView; the capability probe always answers for it:

  ```csharp
  // MauiProgram.cs
  builder.Services.AddMauiBlazorWebView();
  builder.Services.AddBitBmageServices(options =>
  {
      // The pixel loops run on the device's CPU, not in WebAssembly, so give an image every core.
      options.MaxDegreeOfParallelism = Environment.ProcessorCount;
  });
  ```

- **The stylesheet goes in `wwwroot/index.html`**, the page the WebView hosts, as in standalone WebAssembly. Do not add
  a script tag for `bit-bmage.js`: the library imports it on demand, and the codec workers load from the same
  `_content/Bit.Bmage/` folder of the app package.
- **Saving a file is the platform's job on mobile.** `ImageExporter.DownloadAsync` and the editor's download hand the
  file to the WebView. WebView2 on Windows saves it as Edge would; Android's WebView and iOS's WKWebView leave
  downloads to the app, so there encode with `EncodeWithFallbackAsync` (or the editor's `ExportAsync`) and save the
  bytes with the platform's APIs:

  ```csharp
  var encoded = await Exporter.EncodeWithFallbackAsync(image, [ImageFormat.WebP, ImageFormat.Jpeg]);
  var path = Path.Combine(FileSystem.CacheDirectory, "photo" + encoded.Extension);
  await File.WriteAllBytesAsync(path, encoded.Data);
  await Share.Default.RequestAsync(new ShareFileRequest { Title = "Save the image", File = new ShareFile(path) });
  ```

Which formats the WebView codes depends on its engine: WebView2 on Windows and the Android System WebView are
Chromium, and iOS and Mac Catalyst use WKWebView, Safari's engine, with its tighter canvas limits. The probe measures
whichever one the app runs in, so pick formats with `ChooseEncodableFormat` or `EncodeWithFallbackAsync` rather than
by platform. The MAUI template turns on `UseInterpreter` in Debug builds for Hot Reload, which runs the pixel loops
interpreted: judge performance on a Release build.

[`Samples/MAUI`](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bmage/Samples/MAUI/) is a
working MAUI app over the same pages as the Web App samples, and the E2E suite drives it on Windows.

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

Operations spread their rows over `MaxDegreeOfParallelism` threads, which is one by default, and must stay one in the
browser unless the app is built with threads: Blazor WebAssembly runs .NET on the page's thread, and work handed to
another thread could never be waited for there. An application built with `<WasmEnableThreads>true</WasmEnableThreads>`
opts in:

```csharp
builder.Services.AddBitBmageServices(options => options.MaxDegreeOfParallelism = Environment.ProcessorCount);
```

## Web Workers

Decoding, encoding and resizing the editor's working copy run in Web Workers wherever the browser can do them there:
workers with a 2D `OffscreenCanvas`, which Chrome, Edge, Firefox and Safari 16.4 and later all have. The pool starts on
first use with one worker and grows to one per spare core, at most four, while every worker is busy; pixels and files are
transferred to and from it, never copied. The page stays responsive while a photo decodes or encodes, and a batch decodes
and encodes several images at once.

Nothing has to be configured, and nothing breaks where workers cannot run: without `Worker` or `OffscreenCanvas`, when a
Content Security Policy blocks the worker script, or when a worker fails at a piece of work, that work runs on the page's
thread exactly as before. To keep everything on the page's thread, or to cap the pool:

```csharp
builder.Services.AddBitBmageServices(options => options.MaxCodecWorkers = 0);   // or 1, 2, ...
```

The worker is served next to the module as `_content/Bit.Bmage/bit-bmage-worker.js`. A Content Security Policy that
lists sources explicitly must allow it with `worker-src 'self'` (or a `script-src 'self'` it falls back to); blob URLs
are never used for workers.

## What is not used yet

**WebCodecs.** Detected and reported. It offers little over `createImageBitmap` for still images, and its value is in
video.

## Testing your own integration

The browser test project starts the demo application and drives a real browser against it, Chromium by default:

```bash
cd src && dotnet test --project Utilities/Bmage/Tests/Bit.Bmage.Tests.E2E/Bit.Bmage.Tests.E2E.csproj
```

It uses Playwright's bundled browser when installed, falls back to a system Chrome or Edge, and skips with a clear
message when neither is available, so a checkout without browsers still builds and runs the rest of the suite.

Set `BMAGE_E2E_BROWSER` to `firefox` or `webkit` to run the same tests in Playwright's Gecko or WebKit build, after
installing it once with Playwright's `install firefox webkit`. WebKit on Windows and Linux is Playwright's build of the
engine, not Safari: it catches engine differences, but not iOS memory limits or Safari's own policies.

The suite passes in all three. Two tests are inconclusive by design: Firefox empties the data of a synthetic paste
event, so a paste cannot be simulated there, and Playwright's WebKit for Windows has no `OffscreenCanvas`, so codec work
stays on the page's thread, which the worker tests then check instead.
