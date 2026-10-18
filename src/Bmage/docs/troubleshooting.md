# Troubleshooting

## "Could not load the Bmage browser module"

```
ImageCapabilityException: Could not load the Bmage browser module from
'./_content/Bit.Bmage/bit-bmage.js'.
```

Three usual causes:

1. **Called during prerendering.** There is no browser yet. Move browser work to `OnAfterRenderAsync(firstRender)` or an
   event handler.
2. **Static web assets are not being served.** Confirm that
   `/_content/Bit.Bmage/bit-bmage.js` returns JavaScript. A clean rebuild fixes most cases; `dotnet clean`
   then rebuild if a stale `obj` is involved.
3. **A hosted deployment with the wrong base path.** Check the `<base href>` in `index.html`.

## "JavaScript interop is not available yet"

Same root cause as above: the component is not interactive. In a Blazor Web App, a component with no render mode is
static, and static components cannot use interop. Add `@rendermode InteractiveWebAssembly` or `InteractiveAuto`.

## The editor is invisible or one pixel tall

The editor fills its container, and a container with no height collapses.

```razor
<div style="height: 70vh">
    <BmageEditor />
</div>
```

## The editor looks unstyled

The stylesheet is not referenced:

```html
<link rel="stylesheet" href="_content/Bit.Bmage.Editor/bit-bmage-editor.css" />
```

## Photos appear sideways

Something has bypassed the orientation handling. `AutoOrient` is on by default in `DecodeOptions`, and the browser
decoder applies EXIF orientation while decoding. If you set `AutoOrient = false`, apply it yourself:

```csharp
pipeline = pipeline.AutoOrient();
```

If they appear sideways *twice*, the orientation is being applied and then applied again. Do not re-inject the original
EXIF orientation into an exported image whose pixels are already upright; the metadata policies reset it for you.

## Exported JPEGs have a black background

JPEG has no alpha channel, and browsers composite over black. Set the background explicitly:

```csharp
new ImageExportOptions { Format = ImageFormat.Jpeg, Background = Rgba32.White }
```

## "This browser cannot encode Avif"

```
ImageCapabilityException: This browser cannot encode Avif; it produced image/png instead.
```

Working as intended. Browsers silently substitute PNG for formats they cannot write, and the library refuses to pass that
off as what you asked for. Either check first or ask for a fallback chain:

```csharp
await exporter.EncodeWithFallbackAsync(image, [ImageFormat.Avif, ImageFormat.WebP, ImageFormat.Jpeg], options);
```

## "Cannot reach the target size"

```
ImageEncodeException: Cannot reach 5000 bytes for this image (smallest attempt was 8421 bytes).
```

The budget is not achievable within the constraints given. Raise `MaxFileSize`, lower `MinQuality`, reduce the
dimensions, or allow `MaxFileSizeStrategy.QualityThenDimensions` so the encoder may shrink the image.

## Remote images fail to load

```
ImageDecodeException: Could not fetch 'https://example.com/photo.jpg'. The most common cause is a
missing CORS header on the remote server...
```

The browser will not let a page read pixels it cannot access cross-origin. The remote server must send
`Access-Control-Allow-Origin`. There is no client-side workaround; proxy the image through your own server if you cannot
change the remote one.

## The clipboard does not work

Reading the clipboard needs a user gesture and usually a permission prompt, and is unavailable in some browsers and in
insecure contexts. Check `capabilities.SupportsClipboardRead` and offer a file picker as well. Writing is usually limited
to PNG; encode as PNG before copying.

## A large image produces a blank canvas

The image exceeded the browser's canvas limit, which browsers do not report and which fails silently.

```csharp
if (!capabilities.CanRenderCanvas(width, height))
{
    var cap = capabilities.SuggestedMaxWorkingDimension;
    pipeline = pipeline.MaxSize(cap, cap);
}
```

Safari on older iPhones can cap at 4096px, against 32767 on desktop Chrome.

## The tab crashes or reloads on mobile

Memory pressure. In order of effect:

1. Lower `MaxInteractiveDimension` to 1200 or 1500.
2. Set batch `MaxConcurrency` to 1.
3. Dispose every `ImageBuffer` promptly; `using` is your friend.
4. Use `DecodeOptions.MaxSize` so the browser downscales during decoding, before the full-size pixels ever exist.
5. Lower `ImageLimits.MaxPixels` so oversized uploads are refused with a clear message rather than attempted.

## Processing feels slow

Check the obvious first:

- **Enable AOT compilation and measure a published build.** Without `<RunAOTCompilation>true</RunAOTCompilation>`,
  Blazor WebAssembly interprets the pixel loops 10 to 35× slower than compiled code, and `dotnet run` never uses AOT
  even when it is enabled. This outweighs everything else on this list. See
  [performance.md](performance.md#webassembly-compile-ahead-of-time).
- **Cap dimensions before anything else.** `MaxSize(1920, 1920)` early makes everything after it cheaper.
- **Group adjustments together** so they fuse into one pass. A geometric operation in the middle prevents that.
- **Use `Rotate(90)` rather than `Rotate(90.5)`.** Right angles take a lossless path that is about 100× faster.
- **Lower the PNG compression level for previews.** Level 1 is 3.4× faster than level 6.
- **Do not fight the optimiser.** Leave `Optimize` on.

See [performance.md](performance.md) for measured numbers.

## Sliders do not update the image

`onchange` on a range input is not reliable in Blazor when `oninput` is also handled on the same element. Handle
`oninput` and debounce it; you get a live preview as well. The demo application includes a small `Debouncer` for exactly
this.

## `ObjectDisposedException: ImageBuffer`

Something is using a buffer after disposing it. The usual causes:

- A `using` on a buffer that is still referenced by a component parameter.
- Disposing a buffer the library owns. `ImageEditSession.Preview` and `ImageEditSession.Original` belong to the session.
- Disposing a buffer you passed to `BmageEditor.Image` with `takeOwnership: true`.

The rule is simple: you own what you are returned from `LoadAsync`, `ExecuteAsync`, `RenderFullAsync`, `Clone` and
`Snapshot`. You do not own what you read from a property.

## Text renders as blank boxes, or not at all

Using `FontTextRasterizer` with no font registered throws a clear exception. Using it with a CFF (PostScript) OpenType
font also throws, because only `glyf` outlines are supported. Either register a TrueType font, or use
`BrowserTextRasterizer` in a browser, which uses the platform's own text engine and needs no font file.

## Annotations move when I crop

They should. Annotations live in image coordinates, so a crop that shifts the image shifts them with it. If you want an
annotation pinned to a screen position rather than image content, that is a different concept and belongs in your own
overlay rather than in the document.

## Undo does not restore my image

`EditHistory` tracks the document, not the source image. Replacing the source with `ReplaceSource` clears the history
deliberately: an undo across two different images would be meaningless.

## Browser tests are all skipped

The fixture reports why. Common reasons: Playwright's browsers are not installed and no system Chrome or Edge was found,
or the demo application failed to start. Install a browser:

```bash
pwsh src/Bmage/Tests/Bit.Bmage.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

The fixture also falls back to a system Chrome or Edge automatically.

## The published app fails to render after enabling `TrimMode=full`

Symptom: the app works with `dotnet run` and with a normal `dotnet publish -c Release`, but a publish with
`-p:TrimMode=full` shows *An unhandled error has occurred*, and the browser console reports
`No element is currently associated with component`.

This is not caused by Bmage. A stock `dotnet new blazorwasm` application with no packages added fails in exactly
the same way under `-p:TrimMode=full`, because full trimming removes members that Blazor's reflective component
instantiation needs. That is why `TrimMode=full` is not the Blazor default, and the SDK prints
*Optimizing assemblies for size may change the behavior of the app* when you enable it.

**Use the default trimming that `dotnet publish -c Release` already applies.** It trims these packages well: on a
minimal application the three assemblies go from 631 KB to 458 KB, about 155 KB compressed with Brotli, with the
editor rendering and processing images normally.

Rooting the assembly with `<TrimmerRootAssembly Include="Bit.Bmage.Editor" />` does not help. It keeps the assembly
in the output, but the application still fails the same way, for the same reason.

## Still stuck

Open an issue with the browser and version, the hosting model, a minimal repro, and the full exception. The library's
exception messages are written to be specific, so quoting one usually identifies the problem straight away.
