# Bit.BbScanner

A barcode scanner for Blazor, with the decoding written from scratch in C#.

No barcode SDK, no JavaScript scanner library and no runtime dependency beyond `Microsoft.AspNetCore.Components.Web`.
The browser opens the camera and copies pixels; everything after that - luminance conversion, binarization, detection,
Reed-Solomon correction and bit stream parsing - is managed code you can step into.

```razor
<BbScanner Formats="BbBarcodeFormat.QrCode | BbBarcodeFormat.Ean13"
           Region="BbScanRegion.CenteredSquare(0.7)"
           OnScan="HandleScan" />

@code {
    private void HandleScan(BbBarcodeResult result)
        => Console.WriteLine($"{result.Format}: {result.Text}");
}
```

## What it is for

Reading the barcodes an application meets: a QR code on a ticket, the EAN-13 of a product, the Code 128 of a
shipping label, the Data Matrix on a part, the ITF-14 of a carton. From the camera continuously, from a photo the
user picks, or from pixels the application already has - in the browser or on a server.

## Highlights

- **Nine symbologies read to specification**: QR Code, Data Matrix, Code 128 (including GS1-128), Code 39, EAN-13,
  EAN-8, UPC-A, UPC-E and ITF. PDF417 is implemented apart from its symbol character table; see
  [PDF417 status](#pdf417-status).
- **One copy per frame.** JavaScript writes the frame straight into managed memory through a `[JSImport]` memory
  view: no JSON, no base64, no second buffer.
- **No garbage per frame.** Buffers, bit planes, detectors and scan lines are reused across frames, so a frame
  with no barcode in it allocates nothing on the linear path.
- **Misreading is treated as worse than missing**: check digits, guard patterns and repeated reads are verified
  before a value is reported.
- **Camera control**: device selection, front and rear cameras, torch and zoom where the hardware offers them.
- **Still images**: a built-in file picker, and `DecodeImage` for pixels the application sources itself.
- **Scan regions, overlays and theming**: aim with a region, replace the reticle and the messages with render
  fragments, restyle everything through CSS custom properties.
- **Accessible**: a labelled region, real buttons with pressed state, results announced through a polite live
  region, right-to-left layouts, reduced motion and forced colours.
- **A decoder that runs anywhere .NET runs**: `BbBarcodeDecoder` has no dependency on Blazor, JavaScript or a
  camera.

## Quick start

```bash
dotnet add package Bit.BbScanner
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.BbScanner
```

Put a scanner on a page rendered with `InteractiveWebAssembly` (or in a standalone Blazor WebAssembly app):

```razor
<BbScanner OnScan="r => _last = r" />

<p>@_last?.Text</p>

@code {
    private BbBarcodeResult? _last;
}
```

There are no services to register and no stylesheet to reference: the stylesheet the component needs is injected
automatically by the package's Blazor initializer. The full walkthrough is in [getting started](#getting-started).

## Requirements

- .NET 8, .NET 9 or .NET 10
- Blazor WebAssembly for camera scanning: a standalone WebAssembly app, or the WebAssembly pages of a Blazor Web App.
  Under Blazor Server and Blazor Hybrid the component reports `NotSupported`, and `BbBarcodeDecoder` decodes
  pixels in-process instead - see [hosting models](#hosting-models).
- A secure context: the page served over HTTPS, or from `localhost`.

## The demo

The demo site, [bbscanner.bitplatform.dev](https://bbscanner.bitplatform.dev), has a live playground for every
parameter, generated sample barcodes for every symbology, camera control, scan regions, image scanning, an
in-browser decoder benchmark, customisation and error handling.

## How it fits together

```text
Browser                          | Managed
---------------------------------+---------------------------------------------------------
getUserMedia -> <video>          |
canvas.drawImage (scales down)   |
canvas.getImageData              |
        |                        |
        +-- JSImport MemoryView -+--> byte[] (reused, one per session)
                                 |        |
                                 |        +- BbPixelConverter       RGBA -> luminance, optional subsample
                                 |        +- BbHybridBinarizer      8x8 adaptive blocks, noise floor   (matrix codes)
                                 |        |  BbAdaptiveRowBinarizer per-segment thresholds            (linear codes)
                                 |        +- BbDarkRegionFinder     connected ink, for symbols with no finder
                                 |        +- Detectors              finder patterns, edge fitting, line sweep
                                 |        +- BbGridSampler          perspective transform + sampling
                                 |        +- BbReedSolomonDecoder   GF(256) and GF(929)
                                 |        +- Bit stream parsers per symbology
                                 |                 |
                                 |                 +--> BbBarcodeResult
```

Everything ships in one package, `Bit.BbScanner`, under one namespace, `Bit.BbScanner`. The component is `BbScanner`;
every other public type is prefixed `Bb`.

| Part | Contents |
| --- | --- |
| The component | `BbScanner`: parameters, rendering, the camera lifecycle, the frame loop, overlays. |
| The bridge | The complete JavaScript surface, all `[JSImport]`: opening the camera and copying frames. |
| The pipeline | `BbBarcodeDecoder`: frame budget, scratch state, duplicate suppression, diagnostics. |
| Imaging | Luminance planes, bit planes and binarizers. |
| Decoding | One reader per symbology, plus the shared algorithms (Reed-Solomon, grid sampling, perspective). |

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bbscanner.bitplatform.dev/mcp`, so an AI agent writing scanning code reads the library itself - its API,
this guide, its symbologies and the demo's working pages - instead of guessing at an API it half remembers. It is
also served, next to the other bit libraries, through the aggregated `https://bitplatform.dev/mcp` endpoint.

```json
{
  "mcpServers": {
    "bbscanner": { "type": "http", "url": "https://bbscanner.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBbScanner`, which searches the guide, every public type and member, the symbologies, the setup
guides, the stylesheet and the demo's pages at once, and points each hit at the exact follow-up call.
`GetBbScannerSetupGuide` gives the wiring for one render mode, `GetBbScannerGuideSection` one section of this guide,
`GetBbScannerApi` one public type with its members, real defaults and documentation, `GetBbScannerFormats` the
symbologies with the options that steer each one, and `GetBbScannerSourceFile` one of the demo's working pages; each
returns its index when called without a key. `InspectBbScannerSymbol` renders a symbol, decodes it with the
library's own decoder and returns the `BbBarcodeResult` it produced. Every tool is read-only.

## Documentation

- [Getting started](#getting-started)
- [Symbologies](#symbologies)
- [Using the component](#using-the-component)
- [Styling](#styling)
- [Using the decoder without Blazor](#using-the-decoder-without-blazor)
- [Hosting models](#hosting-models)
- [Errors and permissions](#errors-and-permissions)
- [What it reads, and what it does not](#what-it-reads-and-what-it-does-not)
- [Performance](#performance)
- [Architecture](#architecture)
- [PDF417 status](#pdf417-status)
- [Building and testing](#building-and-testing)

## Getting started

### Install

```bash
dotnet add package Bit.BbScanner
```

In a Blazor Web App, add the package to the .Client project: the scanner runs in the browser, so its assembly has
to be downloaded there. Then add the namespace to `_Imports.razor`:

```razor
@using Bit.BbScanner
```

That is the whole setup. There is no service registration, no script tag, no stylesheet link and no
initialisation call: the package's Blazor initializer adds `_content/Bit.BbScanner/bit-bbscanner.css` to the page,
and the component imports its own JavaScript module the first time it renders. Linking the stylesheet yourself
also works - the injection then stands down.

### A first scanner

```razor
@page "/scan"
@rendermode InteractiveWebAssembly

<BbScanner Formats="BbBarcodeFormat.QrCode"
           Region="BbScanRegion.CenteredSquare(0.7)"
           OnScan="HandleScan"
           OnError="HandleError" />

@if (_error is not null)
{
    <p role="alert">@_error.Message</p>
}

@code {
    private BbScannerException? _error;

    private void HandleScan(BbBarcodeResult result)
    {
        _error = null;
        Console.WriteLine($"{result.Format}: {result.Text}");
    }

    private void HandleError(BbScannerException error) => _error = error;
}
```

The camera starts as soon as the component is ready (`AutoStart` is on by default); the browser asks for
permission the first time. The scanner prerenders safely on the server and starts once WebAssembly takes over. In a
standalone Blazor WebAssembly app leave the `@rendermode` line out: everything there already runs on WebAssembly.

### Where to go next

Narrow `Formats` to the symbologies you need - it is the single most effective speed-up - and read
[using the component](#using-the-component) for the camera, still images, regions and customisation.

## Symbologies

| Symbology | Status | Notes |
| --- | --- | --- |
| QR Code | Supported | Versions 1-40, all four error correction levels, all eight masks, ECI, structured append, mirrored symbols |
| Data Matrix | Supported | ECC 200, all 24 square and 6 rectangular shapes, ASCII / C40 / Text / X12 / EDIFACT / Base 256, ECI, structured append, GS1 |
| Code 128 | Supported | Code sets A, B and C, shifts, FNC1 (GS1-128) |
| Code 39 | Supported | Optional modulo 43 check character, optional full ASCII extension |
| EAN-13 | Supported | Check digit verified |
| EAN-8 | Supported | Check digit verified |
| UPC-A | Supported | Reported as UPC-A rather than as a zero-prefixed EAN-13 |
| UPC-E | Supported | Expanded and check-digit verified against its UPC-A form |
| ITF | Supported | Configurable accepted lengths |
| PDF417 | **Partial** | Fully implemented except the symbol character table; see PDF417 status |

`BbBarcodeFormat` is a flags enum, so a set of symbologies is one value: `BbBarcodeFormat.QrCode | BbBarcodeFormat.Ean13`.
`AllLinear`, `AllMatrix` and `All` are the composites, and `All` is the default. Only the readers of the requested
formats are created, so narrowing `Formats` costs nothing and makes every frame cheaper - and it is the main guard
against a misread between similar linear symbologies.

## Using the component

### Parameters

| Parameter | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Formats` | `BbBarcodeFormat` | `All` | Symbologies to look for. A flags enum. |
| `Region` | `BbScanRegion` | `Full` | Normalized sub-rectangle of the frame to search. |
| `Options` | `BbScannerOptions?` | `null` | Advanced tuning, applied under `Formats` and `Region`. |
| `TargetFrameRate` | `int` | `15` | Frames a second to decode. |
| `RequestedWidth` / `RequestedHeight` | `int` | `1280` / `720` | Camera constraints, requested as *ideal*, so the browser negotiates the nearest mode rather than failing. |
| `MaxProcessingSize` | `int` | `640` | Longest edge of the image the decoder sees; the browser scales frames down to it. |
| `Facing` | `BbCameraFacing` | `Rear` | Preferred camera when no device id is given. |
| `DeviceId` | `string?` | `null` | A specific camera to open. Changing it while running switches cameras. |
| `AutoStart` | `bool` | `true` | Open the camera as soon as the component is ready. |
| `DuplicateSuppressionWindow` | `TimeSpan?` | `null` (2 s) | How long an identical value stays suppressed. When set it overrides `Options`; when not, `Options` decides, else 2 s. |
| `ShowControls`, `ShowStatusBar`, `ShowReticle`, `ShowMask`, `HighlightResults` | `bool` | `true` | Built-in chrome. |
| `EnableFileScanning` | `bool` | `true` | Offer still image scanning. |
| `Fill` | `bool` | `false` | Fill the parent instead of keeping the camera's aspect ratio. |
| `AriaLabel` | `string` | `"Barcode scanner"` | Accessible name for the scanner region. |
| `CssClass` | `string?` | `null` | Extra classes for the root element. |
| `Overlay` | `RenderFragment<BbScanner>?` | `null` | Replaces the reticle; receives the scanner. |
| `MessageTemplate` | `RenderFragment<string>?` | `null` | Replaces status and error message rendering. |
| `ControlsContent`, `ChildContent` | `RenderFragment?` | `null` | Extra controls, and content on top of the viewfinder. |

Any other attribute - `id`, `style`, `data-*`, a `class` of your own - is applied to the root element. A `class` or
`style` supplied that way is merged with the component's own rather than replacing it.

### Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `OnScan` | `BbBarcodeResult` | A symbol is decoded and passes duplicate suppression. |
| `OnError` | `BbScannerException` | The camera cannot start, or is lost while running. |
| `OnStatusChanged` | `BbScannerStatus` | The lifecycle state changes. |
| `OnCameraChanged` | `BbCameraInfo` | A camera opens, with what was negotiated. |
| `OnDiagnostics` | `BbScanDiagnostics` | At most twice a second, with frame timings. |

An exception thrown by one of these handlers is routed to the renderer, exactly as Blazor treats an exception from
an `@onclick` handler, rather than being swallowed or killing the frame loop.

### Methods and properties

```csharp
Task              StartAsync(string? deviceId = null)
Task              StopAsync()
void              Pause()
void              Resume()
Task              SwitchCameraAsync(string deviceId)
Task<bool>        SetTorchAsync(bool on)
Task<bool>        SetZoomAsync(double zoom)
Task              OpenFilePickerAsync()
Task<IReadOnlyList<BbCameraDevice>> GetCamerasAsync()
BbBarcodeResult?  DecodeImage(ReadOnlySpan<byte> pixels, int width, int height, BbPixelFormat format)

BbScannerStatus                Status
BbCameraInfo?                  Camera
IReadOnlyList<BbCameraDevice>  Cameras
BbScanDiagnostics              Diagnostics
long                           FramesDecoded
long                           FramesWithSymbol
bool                           IsTorchOn
double                         Zoom
(int Width, int Height)        ProcessingSize
```

`StartAsync` is safe to call at any time and from anywhere: a call that arrives while an earlier one is still
waiting for the permission prompt supersedes it, and the superseded request releases its camera instead of leaving
it running. The same holds for `StopAsync` and for disposal, so a user who navigates away mid-prompt never leaves
the camera indicator lit. `Pause` keeps the camera open and stops decoding, so `Resume` is instant.

### Results

```csharp
public sealed class BbBarcodeResult
{
    public string Text { get; }                            // decoded value
    public ReadOnlyMemory<byte> RawBytes { get; }          // payload before character set interpretation
    public BbBarcodeFormat Format { get; }
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyList<BbScanPoint> Corners { get; }     // in frame pixel coordinates
    public BbScanRect BoundingBox { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public BbBarcodeResultMetadata? Metadata { get; }      // version, EC level, corrections, ECI, GS1, structured append
}
```

Corner points are always reported in the coordinates of the frame the camera produced, not of a cropped scan
region, so an overlay drawn from them lines up with what the user sees. Matrix codes report their corners clockwise
from the top-left; linear codes report the two ends of the scanned line. The built-in highlight also accounts for
a mirrored front camera and for cropping under `Fill`.

### Scan region

`Region` restricts every stage of the pipeline to part of the frame, in normalized coordinates that do not depend
on the resolution the camera negotiated. `BbScanRegion.CenteredSquare(0.7)` - a centred square covering 70 % of
the shorter edge - is the usual choice for an aiming interface; `ShowMask` dims everything outside it and the
reticle frames it. The square stays square in pixels on any camera: it carries `KeepSquare`, and is fitted to each
frame's aspect ratio (`ForFrame`), so on a 1280×720 camera it searches 504×504 pixels. Restricting the region
reduces the work of every frame proportionally, from the grayscale conversion on. Still images scanned through the
file picker are always searched in full.

### Camera, torch and zoom

`Facing` picks the rear or the front camera when no `DeviceId` is set; `GetCamerasAsync` lists the cameras the
browser will open, and setting `DeviceId` (or calling `SwitchCameraAsync`) switches between them without tearing
the component down. Browsers withhold camera labels until permission has been granted once, so a list read before
the first start carries generic names.

Torch and zoom are capabilities of the running track. `Camera` (and `OnCameraChanged`) says what the browser
negotiated - resolution, frame rate, facing mode, and whether the track supports a torch and a zoom range - and the
built-in controls only offer what the hardware accepts. `SetTorchAsync` and `SetZoomAsync` return whether the
browser accepted the request.

### Scanning still images

With `EnableFileScanning` on, the control bar offers a *Scan image* button, and `OpenFilePickerAsync` opens the same
hidden file input from a button of your own. Large photographs are scaled down in the browser before they cross
into managed code: a twelve megapixel image carries no more barcode detail than a two megapixel one. A transparent
image is read as if printed on white, which is what a code generator's PNG means. The outcome - "No barcode found",
or a file that is not an image - is shown for a few seconds without replacing the scanner's own status message.

`DecodeImage` decodes pixels the application already has - from a canvas, an upload or a generated buffer - with
the scanner's symbologies and options. Both paths use the still image tuning, which flips the latency trade-offs a
live frame makes: more scan lines, the exhaustive detector path, every ten degrees of rotation and inverted symbols.

### Duplicates and the lifecycle

The camera produces the same symbol in frame after frame, so an identical value from the same symbology is
suppressed for `DuplicateSuppressionWindow` (two seconds by default) after it was reported, and for as long as it
stays in view. Every symbol seen within the window is remembered, so two codes in view at once are each reported
once, however the frames alternate between them. Set it to `TimeSpan.Zero` to report every frame that decodes.
`Resume` and `StartAsync` reset the filter, so the symbol that was in view when scanning paused is reported again.

The highlight follows a symbol while it stays in view, and goes away a moment after it leaves. A frame the decoder
fails on is skipped rather than ending the session; only a decoder that fails frame after frame faults the scanner.
Options that fail `Validate` are reported as a fault before the camera is opened.

`Status` moves through `Idle`, `Starting`, `Scanning`, `Paused`, `Stopped` and `Faulted`; `OnStatusChanged` reports
each change, and the root element carries it as `data-status`.

### Customising the look

`Overlay` replaces the reticle with your own markup and receives the scanner itself, so it can read the status, the
open camera and the current zoom without extra plumbing. `MessageTemplate` renders the status and error messages,
`ControlsContent` appends controls to the built-in bar, and `ChildContent` is drawn on top of the viewfinder.

```razor
<BbScanner Region="BbScanRegion.CenteredSquare(0.72)">
    <Overlay Context="scanner">
        <div class="crosshair">@(scanner.Status == BbScannerStatus.Scanning ? "Aim at a barcode" : null)</div>
    </Overlay>
    <MessageTemplate Context="message">
        <strong>@message</strong>
    </MessageTemplate>
</BbScanner>
```

## Styling

Every colour, radius and dimension is a CSS custom property, declared in a `:where(.bit-bbscanner)` block so that it
carries no specificity at all. An application therefore restyles the scanner without overriding a single selector,
and without depending on stylesheet order:

```css
.my-scanner {
    --bit-bbscanner-accent: #35c77c;
    --bit-bbscanner-reticle-color: #35c77c;
    --bit-bbscanner-reticle-thickness: 4px;
    --bit-bbscanner-radius: 1.5rem;
    --bit-bbscanner-overlay: rgba(0, 0, 0, 0.7);
}
```

Because the properties are read on the root element, setting them on an ancestor works too; setting them on the
scanner itself (through `style`) always wins. The full list is in
[`bit-bbscanner.css`](Bit.BbScanner/Styles/bit-bbscanner.css) and on the demo's customisation page.

The component is a labelled region, the controls are real buttons with pressed state, results are announced through
a polite live region, and the layout uses logical properties throughout, so a right-to-left document needs no extra
work. Animation is suppressed under `prefers-reduced-motion`. Under `forced-colors` the reticle, highlight and
control borders switch to system colours, focus rings stay visible, and the video and its dimming mask opt out of
colour substitution so the viewfinder is still a viewfinder.

## Using the decoder without Blazor

The pipeline has no dependency on Blazor, JavaScript or a camera. It runs anywhere .NET runs - a server decoding
uploads, a desktop or MAUI application decoding frames from the platform's camera API, a unit test.

```csharp
using var decoder = new BbBarcodeDecoder(new BbScannerOptions
{
    Formats = BbBarcodeFormat.QrCode | BbBarcodeFormat.DataMatrix,
    DuplicateSuppressionWindow = TimeSpan.Zero,
});

var result = decoder.Decode(rgbaPixels, width, height, BbPixelFormat.Rgba32);
if (result is not null)
{
    Console.WriteLine($"{result.Format}: {result.Text}");
    Console.WriteLine($"took {decoder.LastFrame.Total.TotalMilliseconds:0.00} ms");
}
```

`Decode` accepts `Rgba32`, `Bgra32`, `Rgb24` and `Gray8` pixels, or a `BbLuminanceView` over a grayscale buffer.
One instance owns all the scratch state a frame needs and is **not** thread safe; use one per scanning session.
`BbScannerOptions.ForStillImages()` flips the trade-offs for one-shot decoding: more scan lines, every ten degrees of
rotation, the exhaustive detector path, and inverted symbols allowed.

### Tuning

| Setting | Default | Effect |
| --- | --- | --- |
| `Formats` | `All` | Only the decoders that can produce a requested format are created. |
| `Region` | `Full` | Restricts every stage proportionally. |
| `MaxScanLines` | `15` | Scan lines per orientation for linear symbologies. |
| `DownsampleFactor` | `1` | Subsamples while converting to grayscale. Quadratic. |
| `TryHarder` | `false` | Exhaustive detection: every row for finder patterns, more candidate regions, the full rotation sweep. |
| `AllowInverted` | `false` | Also read light-on-dark symbols. Roughly doubles the cost of a frame that finds nothing. |
| `TryReversedRows` | `true` | Also read linear symbols right to left. |
| `TryVerticalLines` | `true` | Also scan columns and tilted lines, so a rotated linear symbol is read. |
| `ItfLengths` | 6-20 even | Digit counts an ITF result may have. ITF has no check character, so this is the main defence against a truncated read. |
| `Code39CheckDigit` | `false` | Require and strip the optional modulo 43 check character. |
| `Code39ExtendedMode` | `false` | Interpret Code 39 as full ASCII. |
| `DuplicateSuppressionWindow` | 2 s | How long an identical value stays suppressed. |

`Validate` throws when a value is outside its supported range, and `Clone` copies an instance so a running scanner
is not affected by later edits.

## Hosting models

Camera scanning runs in the browser: the component reads frames through `[JSImport]` memory views, which only exist
on the WebAssembly runtime. Where it runs decides what is available.

### Blazor WebAssembly

The model the scanner is built for. The component, the frame loop and the decoder all run in the browser, and every
frame crosses from JavaScript into managed memory exactly once. In a standalone WebAssembly app nothing else is
needed: put `<BbScanner />` on a page.

### Blazor Web App

Put the package and the pages that hold a scanner in the .Client project, and render them with
`InteractiveWebAssembly`. The server prerenders those pages - the scanner renders its markup and stays inert - and
the camera starts once WebAssembly takes over. With `InteractiveAuto` the first visit renders on the server, where
the scanner cannot reach the camera, so give the scanning pages `@rendermode InteractiveWebAssembly` explicitly;
the rest of the app can stay on Auto.

### Blazor Server

Under interactive server rendering the component reports `BbScannerErrorKind.NotSupported` - with a message saying
which renderer it is on - rather than showing a Start button that quietly does nothing. Either render the scanning
page with `InteractiveWebAssembly`, or decode on the server: `BbBarcodeDecoder` decodes an uploaded image in-process
once your own code has turned it into pixels (see [using the decoder without Blazor](#using-the-decoder-without-blazor)).

### Blazor Hybrid

In a MAUI, WPF or Windows Forms `BlazorWebView` the component reports `NotSupported` as well. Use the platform's own
camera API and feed its frames to `BbBarcodeDecoder`, which runs in-process.

## Errors and permissions

Camera failures are ordinary and expected, so the component classifies each one as a `BbScannerErrorKind` on the
`BbScannerException` it reports through `OnError`, and sets `Status` to `Faulted`:

| Kind | Cause | Suggested handling |
| --- | --- | --- |
| `PermissionDenied` | The user or a policy refused the camera. | Explain why the camera is needed and offer a retry; the browser will not re-prompt on its own. |
| `NoCamera` | No device matched the request. | Fall back to image scanning or manual entry. |
| `CameraUnavailable` | A device exists but is held by something else, or stopped delivering frames. | Ask the user to close other applications using the camera, then retry. |
| `ConstraintsUnsatisfied` | No camera can meet the requested constraints - typically a `DeviceId` that is gone. | Drop the `DeviceId` and start again with `Facing`. |
| `NotSupported` | No camera API, the page is not a secure context, or the component is not running on WebAssembly. | Serve over HTTPS, render the page on WebAssembly, or hide the scanner. |
| `Unknown` | Anything the browser did not classify. | Log the inner exception and offer a retry. |

Browsers only expose `getUserMedia` over HTTPS or on `localhost`. Served any other way the component reports
`NotSupported` immediately, with a message that says exactly that, instead of hanging on a permission prompt that
will never appear. `RequestedWidth` and `RequestedHeight` are *ideal* constraints, so an unreachable resolution is
negotiated down rather than refused; resolution alone never produces an error. A camera that is unplugged or claimed
by another application while scanning is reported as `CameraUnavailable` rather than leaving the scanner spinning.

## What it reads, and what it does not

The decoders are exercised against transformed renders - rotated, tilted, blurred, noisy, unevenly lit, scaled,
off-centre and on cluttered backgrounds - rather than only against clean round trips. What that establishes:

- **Rotation.** Matrix symbols read at any angle. Linear symbols read within about 30 degrees of upright or
  sideways while scanning live, which is the range a hand actually produces, and at any angle with
  `ForStillImages()`, which sweeps every ten degrees plus both diagonals.
- **Perspective.** Matrix symbols tolerate roughly 30 degrees of tilt.
- **Position.** A symbol anywhere in the frame is found, not only one in the middle.
- **Lighting.** Strong gradients, low contrast and underexposure are handled by the adaptive binarizers; the block
  threshold estimates the frame's noise floor so that grainy blank paper is still recognised as blank.
- **Focus and noise.** Roughly a pixel of blur, and sensor noise up to about a quarter of full scale, on four-pixel
  modules.

What it does not do: read symbols whose modules are below about two pixels, recover from blur much wider than a
module, or read a linear symbol rotated more than 30 degrees during live scanning. Misreading is treated as worse
than missing throughout: ITF, UPC-E and unchecked Code 39 results must repeat on a second scan line before they are
reported, guard patterns must be adjacent rather than merely present, a Reed-Solomon correction is only accepted
when the corrected block re-verifies, and a Data Matrix grid must have a plausible finder and timing pattern before
its codewords are trusted.

Character sets: UTF-8, ISO-8859-1, UTF-16BE and ASCII are always available. QR Kanji and Hanzi segments and the
East Asian ECIs (Shift JIS, GB 2312, Big5, KS X 1001, GB 18030) decode to text when the application has registered
`CodePagesEncodingProvider` before the first scan; without it the text carries replacement characters for those
segments and `RawBytes` carries the exact bytes, so nothing is lost.

## Performance

Measured with BenchmarkDotNet on .NET 10, x64, 640x480 grayscale frames with the symbol centred. Absolute numbers on
WebAssembly are several times larger; the ratios are what transfer.

| Frame | All formats | QR only | Linear only |
| --- | ---: | ---: | ---: |
| QR, 29x29 modules | 603 us | 546 us | 206 us |
| QR, 57x57 modules | 701 us | 646 us | 569 us |
| QR with sensor noise | 702 us | 748 us | 4 509 us |
| Data Matrix | 793 us | 618 us | 222 us |
| Code 128 | 940 us | 695 us | 21 us |
| EAN-13 | 837 us | 697 us | 13 us |
| Empty frame | 876 us | 572 us | 38 us |

The "linear only" column on a frame that holds a matrix symbol is the cost of looking for something that is not
there at every orientation, which is the price of reading tilted symbols. It is only paid when no enabled symbology
matches, and narrowing `Formats` removes it.

Allocation per frame:

| Case | Allocated |
| --- | ---: |
| Linear only, no symbol found | **0 B** |
| QR only, empty frame | 40 B |
| All formats, empty frame | 152 B |
| Linear only, EAN-13 found | 496 B |
| All formats, QR found | 2 552 B |

A frame that finds nothing is the common case while a user is still aiming. It allocates nothing at all on the
linear path, and on the matrix path only the bit plane's wrapper object, whose buffer is pooled. Detectors, pattern
finders and their callbacks are all reused across frames rather than rebuilt, which is what keeps continuous
scanning free of collection pauses.

Where the time goes on a 640x480 frame:

| Stage | Cost |
| --- | ---: |
| Grayscale conversion from RGBA | 328 us |
| Grayscale with 2x subsampling | 120 us |
| Adaptive block binarization | 494 us |
| Binarizing one scan line | 1.0 us |
| QR detection and decode, symbol present | 82 us |
| Data Matrix detection, nothing present | 204 us |
| Code 128 on a row with no symbol | 1.5 us |

The two whole-image stages dominate, which is why `MaxProcessingSize` and `DownsampleFactor` are the controls that
matter most:

| Setting | Effect | When to change it |
| --- | --- | --- |
| `MaxProcessingSize` | Quadratic. The dominant control. | Lower until the smallest symbol you care about stops decoding, then go one step back. |
| `Formats` | Linear in the number of decoders that run. | Always. Most applications need one or two symbologies. |
| `Region` | Proportional to the area kept. | Whenever the interface asks the user to aim. |
| `MaxScanLines` | Linear, linear symbologies only. | Lower for aimed scanning, raise for hands-off scanning. |
| `TryVerticalLines` | Rows only when off; six orientations when on. | Turn off only if symbols are always upright. |
| `TryHarder` | Sweeps eighteen orientations and many more detector candidates. | Still images only; off for live frames by design. |
| `AllowInverted` | Doubles the cost of frames that find nothing. | Only when white-on-black symbols are expected. |

The component itself stays out of the way: frames never enter the render tree, and `ShouldRender` gates on state a
user can actually see, so a scanner running at twenty frames a second renders a handful of times a minute.
`OnDiagnostics` reports the timings of the most recent frame at most twice a second, and costs nothing when nothing
listens. The benchmark suite is in `Tests/Bit.BbScanner.Benchmarks`.

## Architecture

### Design decisions

- **One copy per frame.** `[JSImport]` with a `JSType.MemoryView` parameter hands JavaScript a view straight onto
  the managed buffer. No JSON, no base64, no intermediate array.
- **The browser downsamples.** Frames are scaled to `MaxProcessingSize` by the canvas before they cross the boundary,
  so the expensive stages never see pixels they do not need.
- **Backpressure by construction.** The frame loop asks for the *latest* frame on a timer and gets zero back when
  nothing new has arrived. A slow decode misses ticks rather than building a queue of stale frames.
- **Renders are rare.** Frames never enter the render tree.
- **Detection proposes, decoding disposes.** Both matrix detectors offer several candidate grids - with and without
  the alignment pattern, at neighbouring dimensions, from several patches of ink - and the first one that actually
  decodes wins. Committing to a single geometric guess is what makes a detector fail on symbols that are perfectly
  readable.
- **Failure is not exceptional.** Most frames contain no barcode, so every decode path reports failure by returning
  `null`, never by throwing.

### Extension points

`IBbRowDecoder` adds a linear symbology, `IBbMatrixDecoder` a matrix one, and `IBbBinarizer` a different thresholding
strategy. All three receive already-prepared input, so a new reader only contributes its own tables and structure.

## PDF417 status

PDF417 is implemented end to end - row detection, row indicator interpretation, Reed-Solomon over GF(929), and the
text, byte and numeric compaction modes - with one exception: **the symbol character table**.

That table maps the 2 787 legal seventeen-module patterns to codeword values, and it is pure specification data from
ISO/IEC 15438. It cannot be derived from the structural rules: of the 10 480 patterns that satisfy every structural
constraint, the three usable clusters hold 1 484, 1 002 and 1 002 members, and no linear function of the eight
element widths selects 929 from each; the specification simply lists them.

So the library ships a **generated placeholder** table. It is internally consistent - it round trips against the
matching encoder and exercises every other part of the implementation - but it does not match the specification and
will not read real PDF417 symbols. To avoid pretending otherwise:

- `BbPdf417SymbolTable.Current.IsSpecificationTable` is `false`.
- `BbBarcodeDecoder` does **not** create its PDF417 reader while that is the case, so enabling
  `BbBarcodeFormat.Pdf417` costs nothing and silently finds nothing rather than burning CPU.

Supplying the real table makes PDF417 fully functional:

```csharp
BbPdf417SymbolTable.Register(
    BbPdf417SymbolTable.FromPatterns(cluster0, cluster3, cluster6, isSpecificationTable: true));
```

Each argument is 929 seventeen-bit patterns indexed by codeword value, most significant bit first, starting with a
bar.

## Building and testing

```text
Bit.BbScanner                     the library
Demo/Client                       the demo's pages, run on WebAssembly
Demo/Server                       the demo's host, and the MCP server
Tests/Bit.BbScanner.TestKit       encoders and image helpers, shared by the tests, the benchmarks and the demo
Tests/Bit.BbScanner.Tests         unit, round-trip and image robustness tests
Tests/Bit.BbScanner.Tests.Mcp     the MCP server, over the wire
Tests/Bit.BbScanner.Tests.E2E     browser tests: a real page, a real camera stream
Tests/Bit.BbScanner.Benchmarks    BenchmarkDotNet suites
```

The encoders live in the test kit, not in the library: the library only reads barcodes. They exist so that every
decoder can be validated by round trip instead of by binary fixtures, and so that the demo can render real,
scannable symbols.

From `src/BbScanner` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
dotnet build Bit.BbScanner.slnx
cd Tests/Bit.BbScanner.Tests && dotnet test
dotnet run --project Demo/Server
```

What the tests cover:

- Every symbology round trips from a generated symbol through a rendered image.
- Every symbology is decoded again through camera-like transforms: rotation, perspective, blur, sensor noise,
  lighting gradients, low contrast, inversion, scaling, off-centre placement and cluttered backgrounds.
- Misreads are tested for directly. A truncated ITF or Code 39 symbol, a damaged EAN-13, an out-of-tolerance print
  and twenty thousand random scan lines must all produce nothing.
- Malformed Data Matrix codeword streams - reserved values, a misplaced unlatch, truncated ECI - must be rejected
  rather than partially accepted, and the encodation modes the test kit's encoder cannot emit are driven directly.
- The QR block tables are checked against the published codeword totals for all 40 versions and all four error
  correction levels, and the format and version information codes against published constants.
- The Data Matrix shape table is checked for internal consistency: region tiling, and that the codeword placement
  covers every module of every data region exactly once.
- Reed-Solomon is checked over both fields by damaging codewords and requiring exact recovery.
- The pipeline is checked for zero allocations across repeated empty frames.

`Tests/Bit.BbScanner.Tests.E2E` runs the demo in Chromium whose camera is a generated video file, so the whole stack
is exercised: the WebAssembly runtime, the JavaScript module, a real `MediaStream`, the frame loop and the decoder.
Only the sensor is synthetic. Its [README](Tests/Bit.BbScanner.Tests.E2E/README.md) says what it covers and how to
run it.
