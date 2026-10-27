# Bit.BbScanner

A barcode scanner for Blazor, with the decoding written from scratch in C#.

No barcode SDK, no JavaScript scanner library and no runtime dependency beyond `Microsoft.AspNetCore.Components.Web`.
The browser opens the camera and copies pixels; everything after that - luminance conversion, binarization, detection,
Reed-Solomon correction and bit stream parsing - is managed code you can step into. It runs in every Blazor hosting
model: WebAssembly, Server, Auto and Hybrid (MAUI, WPF and Windows Forms).

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
shipping label, the MaxiCode on a parcel, the Data Matrix or Micro QR on a part, the PDF417 on a driving licence, the
Aztec code on a boarding pass or a rail ticket, the ITF-14 of a carton, the GS1 DataBar on a pack of loose produce and
the DataBar Expanded that carries a pack of meat's weight and price.
From the camera continuously, from a photo the user picks, or from pixels the application already has - in the
browser, on a server or in a native app.

## Highlights

- **Eighteen symbologies read to specification**: QR Code, Micro QR, Data Matrix, PDF417, Aztec, MaxiCode, Code 128
  (including GS1-128), Code 39, Code 93, Codabar, EAN-13, EAN-8, UPC-A, UPC-E (with EAN-2 and EAN-5 add-ons), ITF, GS1
  DataBar (including its stacked forms), DataBar Limited and DataBar Expanded (including Expanded Stacked) - checked
  against symbols written by an independent encoder, not only against the library's own test encoders.
- **Every hosting model.** The same component scans under Blazor WebAssembly, Blazor Server, Auto and Blazor Hybrid.
  In a .NET MAUI app `Bit.BbScanner.Maui` opens the device's own camera - CameraX, AVFoundation, MediaCapture - and
  its frames go straight to the decoder in the app; under Blazor Server the page sends each frame as the luminance of
  the region being searched, and the server decodes it.
- **One copy per frame on WebAssembly.** JavaScript writes the frame straight into managed memory through a
  `[JSImport]` memory view: no JSON, no base64, no second buffer.
- **No garbage per frame.** Buffers, bit planes, detectors and scan lines are reused across frames, so a frame
  with no barcode in it allocates nothing on the linear path.
- **Misreading is treated as worse than missing**: check digits, guard patterns and repeated reads are verified
  before a value is reported.
- **Camera control**: device selection, front and rear cameras, torch and zoom where the hardware offers them.
- **Still images**: a built-in file picker, and `DecodeImage` for pixels the application sources itself.
- **Scan regions, overlays and theming**: aim with a region, replace the reticle and the messages with render
  fragments, restyle everything through CSS custom properties.
- **Accessible and translatable**: a labelled region, real buttons with pressed state and focus that survives
  stopping, results and outcomes announced politely and failures as alerts, every text replaceable through
  `BbScannerTexts`, right-to-left layouts, reduced motion and forced colours. An optional vibration or beep confirms
  a scan.
- **Resilient**: a camera the phone ends when the page goes into the background is reopened when it comes back,
  and the camera can be released whenever the page is hidden.
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

Put a scanner on an interactive page - any render mode, or a standalone WebAssembly or Hybrid app:

```razor
<BbScanner OnScan="r => _last = r" />

<p>@_last?.Text</p>

@code {
    private BbBarcodeResult? _last;
}
```

There are no services to register and no stylesheet to reference: the stylesheet the component needs is injected
automatically by the package's Blazor initializer. A .NET MAUI app adds `Bit.BbScanner.Maui` and one call for the
device's own camera - see [Blazor Hybrid](#blazor-hybrid-net-maui). The full walkthrough is in
[getting started](#getting-started).

## Requirements

- .NET 8, .NET 9 or .NET 10
- An interactive render mode: Blazor WebAssembly, Blazor Server, Auto, or Blazor Hybrid. Prerendered, the scanner is
  inert markup until the page becomes interactive - see [hosting models](#hosting-models).
- A secure context: the page served over HTTPS, or from `localhost`. A `BlazorWebView` is one.
- In a .NET MAUI app, `Bit.BbScanner.Maui` for the device's own camera, and the platform's camera permission - see
  [Blazor Hybrid](#blazor-hybrid-net-maui).
- For camera scanning on WebAssembly in production, the client compiled ahead of time (`RunAOTCompilation`): on
  Blazor's default interpreter a frame costs about a hundred times more - see [performance](#performance).

## The demo

The demo site, [bbscanner.bitplatform.dev](https://bbscanner.bitplatform.dev), has a live playground for every
parameter, generated sample barcodes for every symbology, camera control, scan regions, image scanning, an
in-browser decoder benchmark, customisation and error handling.

[`Samples`](Samples/) is the hosting harness rather than the docs: one shared [Core](Samples/Core/) project of
pages (the live camera, still images, and the decoder reading every symbology in-process), hosted unchanged under
[Server](Samples/Server/), [WASM](Samples/Wasm/), [Auto](Samples/Auto/) and [MAUI](Samples/Maui/) (Blazor Hybrid on
Android, iOS, macOS and Windows), so each [hosting model](#hosting-models) can be seen doing the same things: the
camera, still images, and `BbBarcodeDecoder` in-process.

## How it fits together

```text
Browser (or WebView)             | Managed (in the browser, on a server, or in the app)
---------------------------------+---------------------------------------------------------
getUserMedia -> <video>          |
WebGL: scale + luminance shader  |
  (2D canvas: scale, RGBA)       |
        |                        |
        +-- WebAssembly ---------+--> byte[] (reused, one per session)
        |   JSImport MemoryView, |        |
        |   the whole frame      |        |
        |                        |        |
        +-- Server, Hybrid ------+--------+
            the searched region  |        |
            as luminance, in     |        |
            30 KB slices         |        |
                                 |        +- BbPixelConverter       luminance as is; RGBA -> luminance, optional subsample
                                 |        +- BbHybridBinarizer      8x8 adaptive blocks, noise floor   (matrix codes)
                                 |        |  BbAdaptiveRowBinarizer per-segment thresholds            (linear codes)
                                 |        +- BbDarkRegionFinder     connected ink, for symbols with no finder
                                 |        +- Detectors              finder patterns, bullseyes, edge fitting, line sweep
                                 |        +- BbGridSampler          perspective transform + sampling
                                 |        +- BbReedSolomonDecoder   GF(2^4) to GF(2^12), and GF(929)
                                 |        +- Bit stream parsers per symbology
                                 |                 |
                                 |                 +--> BbBarcodeResult
```

Everything ships in one package, `Bit.BbScanner`, under one namespace, `Bit.BbScanner`. The component is `BbScanner`;
every other public type is prefixed `Bb`.

| Part | Contents |
| --- | --- |
| The component | `BbScanner`: parameters, rendering, the camera lifecycle, the frame loop, overlays. |
| The bridges | One JavaScript module - opening the camera and copying frames - reached through `[JSImport]` on WebAssembly and through `IJSRuntime` everywhere else. |
| The pipeline | `BbBarcodeDecoder`: frame budget, scratch state, duplicate suppression, diagnostics. |
| Imaging | Luminance planes, bit planes and binarizers. |
| Decoding | One reader per symbology, plus the shared algorithms (Reed-Solomon, grid sampling, perspective). |

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bbscanner.bitplatform.dev/mcp`, so an AI agent writing scanning code reads the library itself - its API,
this guide, its symbologies and the demo's working pages - instead of guessing at an API it half remembers.

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
- [Privacy and security](#privacy-and-security)
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

Add the package to the project that holds the pages with a scanner: in a Blazor Web App, the .Client project for
pages rendered with `InteractiveWebAssembly` or `InteractiveAuto` (the server project references it), and the server
project for pages rendered with `InteractiveServer`. Then add the namespace to `_Imports.razor`:

```razor
@using Bit.BbScanner
```

That is the whole setup. There is no service registration, no script tag, no stylesheet link and no
initialisation call: the package's Blazor initializer adds `_content/Bit.BbScanner/bit-bbscanner.css` to the page,
and the component imports its own JavaScript module the first time it renders. Linking the stylesheet yourself
also works - the injection then stands down. The one exception is a .NET MAUI app that scans with the device's own
camera: it adds `Bit.BbScanner.Maui`, calls `builder.UseBbScanner()` and wraps its BlazorWebView in a
`BbScannerHost` - see [Blazor Hybrid](#blazor-hybrid-net-maui).

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
permission the first time. The scanner prerenders safely on the server and starts once the page is interactive.
`InteractiveServer` and `InteractiveAuto` work as well - WebAssembly is the fastest of them, since the frames never
leave the browser. In a standalone WebAssembly app or a Blazor Hybrid app leave the `@rendermode` line out.

### Where to go next

Narrow `Formats` to the symbologies you need - it is the single most effective speed-up - and read
[using the component](#using-the-component) for the camera, still images, regions and customisation.

## Symbologies

| Symbology | Status | Notes |
| --- | --- | --- |
| QR Code | Supported | Versions 1-40, all four error correction levels, all eight masks, ECI, structured append, mirrored symbols |
| Micro QR | Supported | M1 to M4, every error correction level each has, all four masks, numeric, alphanumeric, byte and Kanji segments; `Metadata.SymbolVersion` is `M1` to `M4`. Looked for round the finder patterns the QR Code search found, and only when no QR symbol was read, so it costs QR Code nothing; the grid is laid by the perspective the four sides of its one finder pattern give, its size and angle put right by the timing patterns. Each variant is trusted to repair only as many codewords as the specification gives it - none in M1, whose two check codewords only detect errors |
| Data Matrix | Supported | ECC 200, all 24 square and 6 rectangular shapes, ASCII / C40 / Text / X12 / EDIFACT / Base 256, ECI, structured append, GS1 |
| Code 128 | Supported | Code sets A, B and C, shifts, FNC1 (GS1-128) |
| Code 39 | Supported | Optional modulo 43 check character, optional full ASCII extension |
| Code 93 | Supported | Full ASCII, both modulo 47 check characters verified and stripped |
| Codabar | Supported | Start and stop characters A to D (or T, N, *, E) stripped unless `CodabarStartStop`; no check character, so confirmed on a second scan line |
| EAN-13 | Supported | Check digit verified; EAN-2 and EAN-5 add-ons read into `Metadata.Extension` |
| EAN-8 | Supported | Check digit verified; add-ons read |
| UPC-A | Supported | Reported as UPC-A rather than as a zero-prefixed EAN-13; add-ons read |
| UPC-E | Supported | Expanded and check-digit verified against its UPC-A form; add-ons read |
| ITF | Supported | Configurable accepted lengths |
| PDF417 | Supported | Any orientation, 1-30 columns and 3-90 rows, security levels 0-8, text / byte / numeric compaction, ECI, Macro PDF417 segment numbering, compact (truncated) PDF417; not MicroPDF417 - see [PDF417 status](#pdf417-status) |
| Aztec Code | Supported | Compact (1-4 layers) and full range (1-32 layers), any orientation, every character mode, ECI, GS1 (FNC1), mirrored symbols, and Aztec Runes, reported as their number from 0 to 255; structured append is not interpreted. A symbol written at an encoder's lowest error correction level - three check codewords - is only read undamaged: so few cannot tell a repaired block from a wrong one |
| MaxiCode | Supported | Modes 2 to 5: the structured carrier message of modes 2 and 3 - postal code, country and class of service - reported as the carrier writes it, after the `[)>` header; standard and enhanced error correction; the five code sets with their shifts and locks, the numeric shift and ECI. `Metadata.SymbolVersion` is `Mode 2` to `Mode 5`. Found by its bullseye at any angle, the four diameters of its rings giving the grid's shape and their edges, to a fraction of a pixel, its scale; the grid's foreshortening away from the rings is searched for when it does not read flat. Mode 6, which programs a reader, is not read, and structured append is not interpreted |
| GS1 DataBar | Supported | Omnidirectional, Truncated, Stacked and Stacked Omnidirectional; the GTIN-14 reported as the GS1 element string `01` followed by its fourteen digits, check digit included, with `Metadata.IsGs1` set and `Metadata.SymbolVersion` `Stacked` for a symbol read from two rows. The modulo 79 checksum is verified, and as it is the only check, every read is confirmed on a second scan line; bars printed wider or narrower than their modules are measured on the guards and finder patterns and corrected for. A stacked symbol's rows, and the halves of a short symbol turned further off a line than one line can cross whole, are read on different lines and put together by where they lie - along the symbol's own axis, which the lines' starts trace - and the checksum between them; each half has to be read on two lines and more than twice as often as anything else its place reads as. A module and a half of light is wanted beyond each guard bar, although DataBar itself needs no quiet zone: a symbol printed right up against other ink is missed rather than risked. The linear part of a GS1 Composite symbol is not read |
| GS1 DataBar Limited | Supported | A GTIN-14 starting with 0 or 1, reported as DataBar's is. The modulo 89 check character is verified and every read confirmed on a second scan line; the symbol has no finder pattern, so a candidate has to show its guards, its single-module elements, the five modules of light after it and each character's width before anything is rounded. The linear part of a GS1 Composite symbol is not read |
| GS1 DataBar Expanded | Supported | Expanded and Expanded Stacked, every encodation method: the compressed forms for a GTIN with a weight, a price or a date, and the general purpose field in its numeric, alphanumeric and ISO/IEC 646 modes. Reported as the GS1 element string, with a group separator after every element that is not of a predefined length but the last, and `Metadata.IsGs1` set. The modulo 211 checksum, the character count the check character carries, the finder pattern sequence and the data are verified to the last bit and the padding; a stacked symbol's rows, and a long one turned off the lines, are put together from the lines that read them - only along characters some line read one after the other, so that two labels with the same GTIN are not mixed. Confirmed on a second scan line. The linear part of a GS1 Composite symbol is not read |

`BbBarcodeFormat` is a flags enum, so a set of symbologies is one value: `BbBarcodeFormat.QrCode | BbBarcodeFormat.Ean13`.
`AllLinear`, `AllMatrix` and `All` are the composites. The default is `Common` - QR Code, Data Matrix, Code 128,
Code 39, EAN, UPC and ITF, the symbologies on most products, tickets and labels; PDF417, Aztec, MaxiCode, Micro QR,
Codabar, Code 93 and the GS1 DataBar family are asked for by name, or with `All`. Only the readers of the requested
formats are created, so narrowing `Formats` costs nothing and makes every frame cheaper - and it is the main guard
against a misread between similar linear symbologies.

## Using the component

### Parameters

| Parameter | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Formats` | `BbBarcodeFormat` | `Common` | Symbologies to look for. A flags enum. |
| `Region` | `BbScanRegion` | `Full` | Normalized sub-rectangle of the frame to search. |
| `Options` | `BbScannerOptions?` | `null` | Advanced tuning, applied under `Formats` and `Region`. |
| `TargetFrameRate` | `int` | `15` | Frames a second to decode. |
| `RequestedWidth` / `RequestedHeight` | `int` | `1280` / `720` | Camera constraints, requested as *ideal*, so the browser negotiates the nearest mode rather than failing. |
| `MaxProcessingSize` | `int` | `640` | Longest edge of the image the decoder sees; the browser scales frames down to it. |
| `Facing` | `BbCameraFacing` | `Rear` | Preferred camera when no device id is given. |
| `DeviceId` | `string?` | `null` | A specific camera to open. Changing it while running switches cameras. |
| `AutoStart` | `bool` | `true` | Open the camera as soon as the component is ready. |
| `DuplicateSuppressionWindow` | `TimeSpan?` | `null` (2 s) | How long an identical value stays suppressed. When set it overrides `Options`; when not, `Options` decides, else 2 s. `Timeout.InfiniteTimeSpan` reports each value once for as long as the camera stays open. |
| `MaxSymbolsPerFrame` | `int` | `1` | The most symbols read from one frame or image; more reads every code in view, each reported and outlined on its own. |
| `AfterScan` | `BbAfterScan` | `Continue` | What happens after a reported scan: keep scanning, `Pause` (camera stays open) or `Stop` (camera released). |
| `AcceptResult` | `Func<BbBarcodeResult, bool>?` | `null` | Decides which results are reported; one it turns down is ignored as if it were not there. |
| `Feedback` | `BbScanFeedback` | `None` | `Vibrate`, `Beep` or both on every reported scan, where the browser allows it. |
| `ReleaseCameraWhenHidden` | `bool` | `false` | Close the camera while the page is hidden and reopen it when the page is shown again. |
| `Texts` | `BbScannerTexts?` | `null` (English) | Every label, status, notice, error message and announcement, for another language. |
| `ShowControls`, `ShowStatusBar`, `ShowReticle`, `ShowMask`, `HighlightResults` | `bool` | `true` | Built-in chrome. |
| `EnableFileScanning` | `bool` | `true` | Offer still image scanning: the *Scan image* button and dropping an image onto the scanner. |
| `MaxImageFileSize` | `long` | 32 MB | The largest image file accepted, checked before it is decoded. Zero accepts any size. |
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
Task<BbCameraPermission>             GetCameraPermissionAsync()
void              ResetDuplicateFilter()
BbBarcodeResult?  DecodeImage(ReadOnlySpan<byte> pixels, int width, int height, BbPixelFormat format)
IReadOnlyList<BbBarcodeResult>        DecodeAllInImage(ReadOnlySpan<byte> pixels, int width, int height, BbPixelFormat format, int maxSymbols = 16)
Task<IReadOnlyList<BbBarcodeResult>>  ScanImageAsync(Stream image, int maxSymbols = 16, CancellationToken cancellationToken = default)
Task<IReadOnlyList<BbBarcodeResult>>  ScanImageAsync(ReadOnlyMemory<byte> image, string? contentType = null, int maxSymbols = 16, CancellationToken cancellationToken = default)
Task<IReadOnlyList<BbBarcodeResult>>  ScanCurrentFrameAsync(int maxSymbols = 16)

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
region, so an overlay drawn from them lines up with what the user sees. Matrix codes report their four outer
corners clockwise from the symbol's own top-left - the corner of the QR code that carries no finder pattern is the
third, whichever way up the symbol is held - and linear codes report the two ends of the scanned line. The built-in
highlight also accounts for a mirrored front camera and for cropping under `Fill`.

> Earlier pre-releases reported, for QR codes, the centres of the three finder patterns (and of the bottom-right
> alignment pattern) instead of the corners, and listed Data Matrix corners counter-clockwise. Code that drew
> `Corners` or `BoundingBox` now gets the whole symbol, in the documented order.

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
the first start carries generic names (`BbScannerTexts.UnnamedCamera`, "Camera 1" in English). A camera plugged in
or unplugged while scanning appears in, or leaves, the built-in camera picker without a restart.

Phones end a page's camera when the page goes into the background - another app, a locked screen. A camera ended
while the page was hidden is not reported as lost: `Status` reads `Starting` while the page stays hidden, and the
same camera is opened again, paused if it was paused, when the page is shown. `ReleaseCameraWhenHidden` does the
same on purpose wherever the page is hidden, a background tab included, so the camera indicator goes out and the
battery is spared; the cost is a moment's restart, and a permission prompt again on a browser that did not
remember the user's answer. A camera that is lost while the page is in view is still reported as
`CameraUnavailable`.

Torch and zoom are capabilities of the running track. `Camera` (and `OnCameraChanged`) says what the browser
negotiated - resolution, frame rate, facing mode, and whether the track supports a torch and a zoom range - and the
built-in controls only offer what the hardware accepts. `SetTorchAsync` and `SetZoomAsync` return whether the
browser accepted the request.

### Scanning still images

With `EnableFileScanning` on, the control bar offers a *Scan image* button, an image file dragged onto the scanner
is scanned too, and `OpenFilePickerAsync` opens the same hidden file input from a button of your own. Large
photographs are scaled down in the browser before they cross into managed code: a twelve megapixel image carries no
more barcode detail than a two megapixel one. A transparent image is read as if printed on white, which is what a
code generator's PNG means. The outcome - "No barcode found", or a file that is refused - is shown for a few seconds
without replacing the scanner's own status message. None of this opens the camera, so a page that only scans
images (`AutoStart="false"`) never asks for camera permission.

Files are untrusted input, so each is checked before it is decoded: one that declares a type other than an image is
refused, as is one larger than `MaxImageFileSize` (32 MB by default), and an image that decodes to more than about
268 megapixels - a small file can declare an enormous image - is refused before its pixels are copied anywhere.

An image the application already holds goes to `ScanImageAsync`: the bytes of a PNG, JPEG, WebP or any format the
browser decodes, from a `Stream` or from memory. It is decoded in the browser exactly like a picked file, under the
same limits, and returns every symbol in it rather than raising `OnScan`; a refused or undecodable image throws a
`BbScannerException` of kind `InvalidImage`. Under Blazor Server the browser is across the network, so a stream the
server holds is sent there and its luminance comes back: an `InputFile` upload crosses the network twice, where a file
chosen through the built-in picker is decoded where it was chosen and sent once. While the page is not interactive
yet - prerendering - it throws `NotSupported` before reading a byte of the stream. With Blazor's `InputFile`:

```razor
@* Only images: the camera never starts, and no permission is asked for. *@
<BbScanner @ref="_scanner" AutoStart="false" ShowControls="false" />
<InputFile accept="image/*" OnChange="ScanAsync" />

@code {
    private BbScanner _scanner = default!;

    private async Task ScanAsync(InputFileChangeEventArgs args)
    {
        // Zero is no limit to the scanner, but no bytes at all to OpenReadStream.
        var limit = _scanner.MaxImageFileSize > 0 ? _scanner.MaxImageFileSize : long.MaxValue;
        await using var stream = args.File.OpenReadStream(limit);
        foreach (var result in await _scanner.ScanImageAsync(stream))
        {
            Console.WriteLine($"{result.Format}: {result.Text}");
        }
    }
}
```

`DecodeImage` and `DecodeAllInImage` decode pixels the application already has - from a canvas or a generated
buffer - with the scanner's symbologies and options. `ScanCurrentFrameAsync` reads the camera's current frame at the
camera's own resolution, for a code too dense or too far away for the live frames, which are scaled down to
`MaxProcessingSize`; it makes a good "capture" button beside the viewfinder. All of these use the still image
tuning, which flips the latency trade-offs a live frame makes: more scan lines, the exhaustive detector path, every
ten degrees of rotation and inverted symbols.

### Several symbols at once

`MaxSymbolsPerFrame` above one reads every symbol in view - a sheet of tickets, a carton with several labels, a shelf
- and reports each through `OnScan` with its own outline; still images follow it too. A matrix symbol once read is
painted out of a working copy of the frame and the frame searched again, so each pass finds the next one, and linear
symbols are gathered from every scan line in one pass; two copies of the same code in different places are two
results. The cost is one more matrix pass than reading a single symbol - the pass that finds nothing else - so the
default stays at one.

### Duplicates and the lifecycle

The camera produces the same symbol in frame after frame, so an identical value from the same symbology is
suppressed for `DuplicateSuppressionWindow` (two seconds by default) after it was reported, and for as long as it
stays in view. Every symbol seen within the window is remembered, so two codes in view at once are each reported
once, however the frames alternate between them. Set it to `TimeSpan.Zero` to report every frame that decodes.
`Resume` and `StartAsync` reset the filter, so the symbol that was in view when scanning paused is reported again.
An EAN or UPC add-on is part of the value, so two issues of a magazine that share the main symbol are each reported;
a frame that reads the add-on an earlier one missed reports the symbol again, with `Metadata.Extension` set, so set
`RequireEanExtension` where each item must be reported once and with its add-on.

`Timeout.InfiniteTimeSpan` turns the window into a session: each value is reported once for as long as the camera
stays open - a stocktake, a check-in desk - remembering up to 4 096 values. `StartAsync` on a scanner with no camera
open and `ResetDuplicateFilter` start a new session; `Resume`, switching cameras and a camera reopened after the page
was hidden do not, since the point is that a value already counted stays counted.

`AfterScan` decides what a reported scan does to the scanner: `Continue` (the default), `Pause`, which stops
decoding and keeps the camera open so a "scan another" button's `Resume` is instant, or `Stop`, which releases the
camera. `AcceptResult` filters before any of that happens: a result it turns down is not reported, outlined or
announced, and does not count as a scan, so a page that expects a ticket number can ignore every other code in view.

```razor
<BbScanner AfterScan="BbAfterScan.Stop"
           AcceptResult="@(r => r.Text.StartsWith("TICKET-", StringComparison.Ordinal))"
           OnScan="CheckIn" />
```

The highlight follows a symbol while it stays in view, and goes away a moment after it leaves. A frame the decoder
fails on is skipped rather than ending the session; only a decoder that fails frame after frame faults the scanner.
Options that fail `Validate` are reported as a fault before the camera is opened.

`Status` moves through `Idle`, `Starting`, `Scanning`, `Paused`, `Stopped` and `Faulted`; `OnStatusChanged` reports
each change, and the root element carries it as `data-status`.

`Feedback` adds a short vibration (`BbScanFeedback.Vibrate`), a short tone (`Beep`) or both to every reported scan -
once per frame, however many symbols it reported. The tone is synthesised with the Web Audio API, so nothing is
downloaded. Both are best effort: browsers only let a page make a sound after the user has interacted with it, so a
scanner that starts on its own can be silent until the first tap, and Safari does not vibrate at all. The outline and
the screen reader announcement are always there as well, so neither is ever the only signal.

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

### Language and accessibility

Every text the scanner shows or speaks comes from a `BbScannerTexts`: the button labels, the accessible names of
the camera picker and the zoom slider, the status, the still image notices, the message shown when it fails and
what a screen reader announces for a scan. Set the ones a language needs - anything left out stays English - and
share one instance per language between every scanner. Texts that carry a value are functions, so word order,
number formatting and plurals stay the translator's:

```razor
<BbScanner Texts="German" />

@code {
    private static readonly BbScannerTexts German = new()
    {
        StartButton = "Starten",
        StopButton = "Beenden",
        PauseButton = "Pause",
        ResumeButton = "Fortsetzen",
        ScanImageButton = "Bild scannen",
        StatusScanning = "Scannen",
        NoBarcodeFound = "Kein Barcode im Bild gefunden.",
        ErrorMessage = e => e.Kind switch
        {
            BbScannerErrorKind.PermissionDenied => "Der Zugriff auf die Kamera wurde verweigert.",
            BbScannerErrorKind.NoCamera => "Keine Kamera gefunden.",
            _ => "Die Kamera konnte nicht gestartet werden.",
        },
        ScanAnnouncement = r => $"{r.Format} gelesen: {r.Text}",
    };
}
```

The strings can as well come from an `IStringLocalizer`; build the instance once the culture is known. `ErrorMessage`
only changes what the user sees: `OnError` still receives the exception with its own message. A text function that
throws falls back to English and its exception is routed to the renderer, so a translation mistake cannot stop the
scanner. The layout follows the document's direction, so a right-to-left language needs `dir="rtl"` on the page or
an ancestor and nothing else. The demo's customisation page switches between English, Spanish and Persian.

For assistive technology, the scanner is a region named by `AriaLabel`; its controls are real buttons and inputs,
reachable and operable from the keyboard with a visible focus ring, and Start and Stop are one button, so stopping
from the keyboard leaves focus where it was. Two live regions are always in the document: a polite one announces the
camera opening, every reported scan - the same value scanned twice is announced twice - and the outcome of a still
image, and an alert announces a failure. Frames are never announced. The highlight is not the only sign of a scan,
and colour is never the only sign of a state: the status bar spells it out.

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

The layout uses logical properties throughout, so a right-to-left document needs no extra work (see
[language and accessibility](#language-and-accessibility)). Animation is suppressed under `prefers-reduced-motion`. Under `forced-colors` the reticle, highlight and
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

`DecodeAll` returns every symbol in the frame instead of the first, up to a limit:

```csharp
foreach (var label in decoder.DecodeAll(rgbaPixels, width, height, BbPixelFormat.Rgba32, maxSymbols: 16))
{
    Console.WriteLine($"{label.Format} at {label.BoundingBox}: {label.Text}");
}
```

`Decode` and `DecodeAll` accept `Rgba32`, `Bgra32`, `Rgb24` and `Gray8` pixels, or a `BbLuminanceView` over a grayscale buffer.
One instance owns all the scratch state a frame needs and is **not** thread safe; use one per scanning session.
`BbScannerOptions.ForStillImages()` flips the trade-offs for one-shot decoding: more scan lines, every ten degrees of
rotation, the exhaustive detector path, and inverted symbols allowed.

### Tuning

| Setting | Default | Effect |
| --- | --- | --- |
| `Formats` | `Common` | Only the decoders that can produce a requested format are created. |
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
| `CodabarStartStop` | `false` | Keep Codabar's start and stop characters in the text. |
| `RequireEanExtension` | `false` | Report EAN/UPC symbols only together with their EAN-2 or EAN-5 add-on. |
| `DuplicateSuppressionWindow` | 2 s | How long an identical value stays suppressed. |

`Validate` throws when a value is outside its supported range, and `Clone` copies an instance so a running scanner
is not affected by later edits.

## Hosting models

What the hosting model decides is where .NET runs relative to the camera, and so how a frame reaches the decoder. In
a browser the camera can only be the page's; in a Blazor Hybrid app it can be the device's own, which .NET opens
itself. The component, its parameters and its events are the same everywhere, and so is everything it shows.

| | Where the decoder runs | How a frame gets there |
| --- | --- | --- |
| Blazor WebAssembly | In the browser | Copied once, by the browser, into managed memory through a `[JSImport]` memory view |
| Blazor Server | On the server | The searched region's luminance, read from the page over the circuit |
| .NET MAUI, with `Bit.BbScanner.Maui` | Natively, in the app | From the device's camera - CameraX, AVFoundation, MediaCapture - straight to the decoder; the page only hosts the overlays |
| Other BlazorWebViews (WPF, Windows Forms) | Natively, in the app | The searched region's luminance, read from the WebView's camera over its message channel |

### Blazor WebAssembly

The fastest of them. The component, the frame loop and the decoder all run in the browser, and every frame crosses
from JavaScript into managed memory exactly once. In a standalone WebAssembly app nothing else is needed: put
`<BbScanner />` on a page. Compile the client ahead of time for production - see [performance](#performance).

### Blazor Web App

Any of the interactive render modes works, per page or for the whole app. Reference the package from the project
whose pages hold a scanner: the .Client project for `InteractiveWebAssembly` and `InteractiveAuto` pages, the server
project for `InteractiveServer` ones. The server prerenders the pages - the scanner renders its markup and stays inert
- and the camera starts once the page is interactive. Under `InteractiveAuto` the first visit runs on a circuit, as
under Blazor Server, and later ones in the browser, with nothing on the page changing between the two.

### Blazor Server

The component runs on the circuit and reaches the page's JavaScript through `IJSRuntime`. On every tick of the frame
loop it asks the page, in one call, what changed and whether a new frame is in; when one is, the page reads it, cuts
it down to the luminance of the [scan region](#scan-region) - a byte a pixel - and the server reads that back in
slices of 30 KB, several at once, and decodes it. A 640x360 frame searched through `BbScanRegion.CenteredSquare(0.7)`
is about 63 KB, where the RGBA frame would be 900 KB; the whole frame, searched in full, is 230 KB. A slice fits in one
message, so the circuit's 32 KB limit on a message from the page (`HubOptions.MaximumReceiveMessageSize`) does not need
raising, and each is an ordinary call with a timeout of its own, so one lost to a reconnecting circuit is simply asked
for again.

What that means for an application:

- **Each scanner costs the server what decoding costs anywhere** - a few hundred microseconds to a couple of
  milliseconds a frame (see [on .NET](#on-net)) - for as long as it is scanning. A paused or stopped scanner decodes
  nothing.
- **The frame rate is set by the connection as much as by `TargetFrameRate`.** A frame takes a poll and its slices, a
  few round trips, and the loop waits for one before asking for the next, so a slow connection lowers the rate rather
  than queueing frames. Aim with a `Region`, which is what cuts a frame down most, and on a slow link lower
  `MaxProcessingSize` (to 480, say) or `TargetFrameRate` (to 8).
- **A camera the circuit leaves behind is closed by the page.** The page keeps a watchdog on the camera it opened:
  when the server has not asked for a frame for 20 seconds - a circuit that dropped for good, a server that went away
  - it closes the camera itself, since nothing on the server can any more. A circuit that reconnects carries on; if
  the watchdog closed the camera meanwhile, the scanner opens it again, as it does for a phone that ended it in the
  background. The frame loop waits on the application's handlers - `OnScan` among them - so one that holds it for
  longer than that, waiting on a dialog say, has the camera closed and opened again too: show the dialog without
  awaiting it in the handler, or pause the scanner first.
- **Still images are decoded in the browser.** The built-in picker and a dropped file are decoded where they were
  chosen and only their luminance is sent; `ScanImageAsync` sends a stream the server holds to the browser first.

### Blazor Hybrid: .NET MAUI

A Blazor Hybrid app is not a web page: .NET runs natively on the device, and can open the camera itself. The
`Bit.BbScanner.Maui` package does, through each platform's own camera API - CameraX on Android, AVFoundation on iOS
and Mac Catalyst, MediaCapture on Windows - and hands the scanner its frames in the app's own process: the luminance
plane, a byte a pixel, upright, at the camera's own resolution and scaled by the scanner to `MaxProcessingSize`. No
frame crosses into the page or back, the decoder runs at full native speed on a thread of its own, and the
platform's own lifecycle handling applies - the camera closes while the app is in the background and opens again when
it returns, and the platform's own permission prompt is the one the user sees.

```bash
dotnet add package Bit.BbScanner.Maui
```

```csharp
// MauiProgram.cs
builder.UseMauiApp<App>()
       .UseBbScanner();
```

```xml
<!-- MainPage.xaml: xmlns:bb="clr-namespace:Bit.BbScanner;assembly=Bit.BbScanner.Maui" -->
<bb:BbScannerHost>
    <BlazorWebView HostPage="wwwroot/index.html">
        ...
    </BlazorWebView>
</bb:BbScannerHost>
```

The Razor does not change: `<BbScanner>` on a page is the same component, and finds the device's camera registered.

**The preview.** A native camera's preview cannot be drawn inside a web page, so the `BbScannerHost` draws it behind
the WebView, where the scanner's video element is, and the page lets it show through, with the scanner's reticle,
mask, highlights and controls drawn over it as usual. While a camera is open the scanner and every element above it in
the page paint no background, the WebView is transparent, and the host paints the page's own background colour behind
it, so the page around the scanner looks as it did - except that an element above the scanner with a colour of its own,
a card it sits in say, shows the page's colour instead until the camera closes. The page reports where the scanner is
on every animation frame it moves on, and the preview follows; while the page scrolls it lags by a frame or so. The
preview is cut to whatever clips the scanner in the page, a scrolling panel say.

Only the scanner's own ancestors are made transparent, so anything else the page paints under the scanner covers the
preview - the page content and the backdrop beneath a dialog the scanner is in, for one. Put the scanner in the page
itself rather than over other content. Without a `BbScannerHost` loaded the scanner opens the WebView's camera
instead; with more than one, a camera draws in the one loaded last.

**Permissions.** Each platform asks the user itself, once the app declares that it uses the camera:

- **Android**: `<uses-permission android:name="android.permission.CAMERA" />` in `AndroidManifest.xml`; the runtime
  permission is asked for the first time a scanner starts.
- **iOS and Mac Catalyst**: `NSCameraUsageDescription` in `Info.plist`, and on Mac Catalyst, with App Sandbox on, the
  `com.apple.security.device.camera` entitlement.
- **Windows**: `<DeviceCapability Name="webcam" />` in `Package.appxmanifest`; a camera turned off in Windows' privacy
  settings is reported as `PermissionDenied`.

The [MAUI sample](Samples/Maui/) is set up this way on all four.

### Blazor Hybrid: WPF and Windows Forms

Without a camera of the platform's own registered - in a WPF or Windows Forms `BlazorWebView`, or a MAUI app without
`Bit.BbScanner.Maui` - the scanner opens the WebView's camera, as it would a browser's, and the WebView sends each
frame's searched region over its message channel, the way a Server page sends it over the circuit, in large slices
since nothing crosses a network. WebView2 asks the user for the camera itself. A WebView that exposes no camera API to
the page is reported as `NotSupported`, like a browser that has none.

Any host can supply its own camera the same way `Bit.BbScanner.Maui` does: register an `IBbCameraSource`, whose
sessions hand over luminance frames and draw their own preview where the scanner says its video element is.

## Errors and permissions

Camera failures are ordinary and expected, so the component classifies each one as a `BbScannerErrorKind` on the
`BbScannerException` it reports through `OnError`, and sets `Status` to `Faulted`:

| Kind | Cause | Suggested handling |
| --- | --- | --- |
| `PermissionDenied` | The user or a policy refused the camera. | Explain why the camera is needed and offer a retry; the browser will not re-prompt on its own. |
| `NoCamera` | No device matched the request. | Fall back to image scanning or manual entry. |
| `CameraUnavailable` | A device exists but is held by something else, or stopped delivering frames. | Ask the user to close other applications using the camera, then retry. |
| `ConstraintsUnsatisfied` | No camera can meet the requested constraints - typically a `DeviceId` that is gone. | Drop the `DeviceId` and start again with `Facing`. |
| `NotSupported` | The page - the browser, or a Hybrid app's WebView - exposes no camera API, the page is not a secure context, or the scanner's script could not be loaded. | Serve over HTTPS, check that static web assets are served, or hide the scanner. |
| `InvalidImage` | An image given to `ScanImageAsync` is not an image, is larger than `MaxImageFileSize`, decodes to too many pixels, or could not be decoded. | Tell the user which; the built-in picker shows the same reasons as a notice. |
| `Unknown` | Anything the browser did not classify. | Log the inner exception and offer a retry. |

Browsers only expose `getUserMedia` over HTTPS or on `localhost`. Served any other way the component reports
`NotSupported` immediately, with a message that says exactly that, instead of hanging on a permission prompt that
will never appear. `RequestedWidth` and `RequestedHeight` are *ideal* constraints, so an unreachable resolution is
negotiated down rather than refused; resolution alone never produces an error. A camera that is unplugged or claimed
by another application while scanning is reported as `CameraUnavailable` rather than leaving the scanner spinning;
one the browser ends because the page went into the background is reopened instead (see
[camera, torch and zoom](#camera-torch-and-zoom)). The message shown for each kind can be translated through
`BbScannerTexts.ErrorMessage`.

`GetCameraPermissionAsync` asks the browser what a start would do - `Prompt`, `Granted` or `Denied` - without asking
the user, so a page can explain why it needs the camera before the prompt appears, or say how to allow it in the
site settings once it has been refused. Where the browser will not say (an older Firefox, say) it returns `Unknown`,
and starting the camera is the only way to find out. A browser that grants the camera for one visit at a time can
report such a grant as `Granted`.

A page inside a cross-origin `<iframe>` needs the camera delegated to it - `<iframe allow="camera">` - and a
`Permissions-Policy` header that forbids the camera makes every start fail with `PermissionDenied`.

## Privacy and security

- **Frames go where the application runs, and nowhere else.** On WebAssembly frames are copied from the video
  element into the application's own WebAssembly memory and decoded there, and in a Blazor Hybrid app they go from
  the camera to the app itself: either way nothing leaves the device. Under Blazor Server the application runs on the server, so that is
  where frames are decoded: the searched region of each frame, in grayscale, crosses the circuit to the server - as
  everything a Server app shows and reads does - and a still image chosen through the picker is decoded in the browser
  and its grayscale sent the same way. The library makes no network request of its own - the only files it loads are
  its script and its stylesheet, from the application's own `_content/Bit.BbScanner/` - and it has no telemetry. A
  value read is only ever handed to the application's `OnScan`.
- **Nothing is recorded or kept.** The camera stream is never recorded. Frames pass through buffers that are reused
  from one frame to the next - the frame itself and the decoder's grayscale, masked and thresholded copies of it, its
  block statistics and its scan lines - and when the camera closes (a stop, a fault, a page hidden with
  `ReleaseCameraWhenHidden`, disposal) every one of them is zeroed, not just let go of, since freed memory keeps its
  contents until it is reused. The browser-side readback array is zeroed and its canvas emptied, and the WebGL context
  that drew the frame is released, as is the region copy a Server or Hybrid page sends. Buffers the decoder hands back
  to .NET's shared array pools go back cleared, so no other code in the application can rent a frame. A picked or captured still image is zeroed as soon as it has been
  decoded, in the browser and in .NET; one handed over as encoded bytes (`ScanImageAsync`) waits in the browser as
  a Blob, which cannot be overwritten, and is released once decoded. The decoder keeps no history of values beyond
  the duplicate filter, which holds values, not images: it is cleared on every start the application asks for, and by
  `ResetDuplicateFilter`. A camera the browser ended in the background and the scanner reopened carries on the same
  session, as `Resume` does: under an infinite `DuplicateSuppressionWindow` nothing is reported twice across it.
- **Untrusted images are bounded.** A file over `MaxImageFileSize` (32 MB by default) is refused before it is
  decoded, a file that declares itself to be something other than an image is refused, an image that decodes to more
  than about 268 megapixels is refused before its pixels are copied, and large images are scaled to 1 600 pixels
  before they are scanned. An image built to defeat the search - tiled with thousands of QR finder patterns or Aztec
  bullseyes, each of which every other has to be weighed against - is given up on after a few dozen of them: such a
  3000 pixel image took seven and a half minutes, and now takes half a second. A pair of start and stop shaped stripes
  is a PDF417 candidate on every frame it is in, and resampling one upright to read cost a large share of a second a
  frame; the candidates of a frame now share a budget of a frame and a half of pixels (eight for a still image).
- **Barcode contents are untrusted input.** A value is whatever someone printed. The component renders it as text
  only - in the live region and nowhere else - and never navigates to a URL, opens a link or runs anything it reads.
  Do the same: render values with Razor's default encoding rather than `MarkupString`, and before acting on a URL,
  check its scheme against an allow list (`https` only, say) and show the user where it leads.
- **Content Security Policy.** The script is a module served from the application itself and uses no `eval`, so
  `script-src 'self'` covers it (Blazor WebAssembly itself also needs `'wasm-unsafe-eval'`). The overlays are
  positioned with inline `style` attributes, so `style-src` (or `style-src-attr`) has to allow `'unsafe-inline'`.
- **Secure contexts only.** The camera is only offered to pages served over HTTPS or from `localhost`; elsewhere the
  scanner says so instead of waiting for a prompt that will never come. Still image scanning works either way.
- **No camera outlives its application.** A camera opened for a Blazor Server circuit is closed by the page once the
  server stops asking for frames, so a server that goes away does not leave the camera light on.

## What it reads, and what it does not

The decoders are exercised against transformed renders - rotated, tilted, blurred, noisy, unevenly lit, scaled,
off-centre and on cluttered backgrounds - rather than only against clean round trips. What that establishes:

- **Rotation.** QR Code - every version, up to 40 - square Data Matrix up to 144 by 144, and PDF417 read at every
  orientation from three pixels a module up, and PDF417 from two; the reference symbols are swept round the full
  circle to check it. At two pixels a module a turned QR code, Data Matrix or Aztec symbol comes out of the binariser
  with its finder patterns' edges a pixel out on nearly every line, so when a frame gives the matrix readers nothing
  they look again at it enlarged twice by interpolation: all of a still image up to a megapixel, the middle of a
  larger one, and on a live camera the middle quarter of every fourth frame that held nothing. Swept round 51 angles
  at two pixels a module, every QR reference symbol up to 57 modules then reads at every angle - from a still image,
  and live within four frames - where a fifth of the angles were lost before; Data Matrix 1 001 of 1 020 renders,
  the thinnest rectangle, 8 by 32, missing most of the rest; Aztec 1 810 of 1 836, the four layer compact symbol
  missing most. The 8 by 32 Data Matrix also misses a few orientations at three and four pixels a module. Aztec Code,
  compact and full range up to its largest, 151 by 151 modules, reads at every orientation from three pixels a
  module, with the camera settings as well as the still image ones. MaxiCode reads at every angle from three pixels a
  module and Micro QR from two: swept round 36 angles at three, four and six pixels a module, all 756 renders of the
  seven MaxiCode reference symbols read from a still image and live within four frames; at two, three and four, 862
  of the 864 of the eight Micro QR symbols from a still image and all of them live - with every symbology enabled, and
  not one misread. Linear symbols
  read within about 30 degrees of upright while scanning live, which is the range a hand actually produces, and at
  any angle with `ForStillImages()`, which sweeps every ten degrees plus both diagonals. Sideways - bars running
  across the frame - live scanning tries vertical lines and lines fifteen degrees either side of them, so how far off
  sideways a symbol reads depends on its proportions: about 15 degrees for a 19 character Code 128 at two pixels a
  module, about 30 for an EAN-13. Between those angles each live frame also tries one of 45, 135, 60 and 120 degrees,
  a different one each frame, so a linear symbol held at any angle at all is read within four frames - a tenth of a
  second at thirty frames a second - for the cost of one more angle a frame. How far off its axis a line can cross a
  linear symbol depends on how tall it is: GS1 DataBar Omnidirectional, 33 modules tall, reads live in every frame at
  every angle but some ten degrees around the diagonals, and there within four frames, from two pixels a module, whereas Truncated DataBar, 13 modules tall, reads within about 35 degrees of upright and
  20 of sideways from three pixels a module, and at two pixels a module leaves gaps between the angles the live scan
  tries. Both read at every angle from a still image. A DataBar symbol no single line crosses whole - the two rows of
  a stacked one, the rows of DataBar Expanded Stacked, or a short or long symbol turned further off a line than one
  line can cross it - is pieced together from what different lines read of it. A live frame follows a line that read
  part of such a symbol with lines either side of it, then lines turned about it, and then lines along the symbol's
  own axis, which the starts of those lines trace; and the live lines shift across by a fraction of their spacing from
  frame to frame, so that a symbol thinner than that spacing is crossed within four frames. Over two to four pixels a
  module, nine angles to 240 degrees, blur to 1.3 pixels and noise to 45 (four live frames, or a still image):
  Omnidirectional 96 % of renders, Stacked Omnidirectional and Stacked together 86 % - Stacked's rows are five and
  seven modules tall, and live it is missed around 50 degrees, which no frame's lines run close enough to - DataBar
  Limited, ten modules tall, 88 %, DataBar Expanded 92 % and Expanded Stacked 68 %, its rows at 30 to 50 degrees,
  blurred and noisy, mostly lost. None of those 12 636 decodes was a misread, nor any of 1 395 values read off
  sheets of four DataBar symbols close together, 919 of them off labels of one product carrying the same GTIN.
- **Perspective.** Matrix symbols tolerate roughly 30 degrees of tilt. An Aztec symbol is fitted through its reference
  grid when it is large enough to have one, and through its outline when it is not. Tilted 30 degrees, every Aztec
  reference fixture reads at every rotation tried; tilted 40, all but one of 102 renders. The outline needs a quiet
  zone, which the specification does not insist on: framed by other ink right up to its edge, the largest compact
  symbol still reads square on and tilted 15 degrees, but only at 8 of 12 rotations tilted 30. MaxiCode has no
  outline or corners to fit: its grid is the ellipse its rings make, and how it is foreshortened away from them is
  searched for, the foreshortening taken being the one under which the modules' samples lie furthest from the
  threshold. At four pixels a module, turned to twelve angles, 83, 84 and 84 of the 84 MaxiCode renders read tilted
  20, 30 and 40 degrees; Micro QR, laid by its finder pattern's four sides, 96, 94 and 91 of 96.
- **Position.** A symbol anywhere in the frame is found, not only one in the middle.
- **Lighting.** Strong gradients, low contrast and underexposure are handled by the adaptive binarizers; the block
  threshold estimates the frame's noise floor so that grainy blank paper is still recognised as blank.
- **Focus and noise.** Roughly a pixel of blur, and sensor noise up to about a quarter of full scale, on four-pixel
  modules.

What it does not do: read symbols whose modules are below about two pixels, recover from blur much wider than a
module, or read in every frame a linear symbol rotated more than about 30 degrees from upright, or more than 15 to 30
from sideways - one held there is read within four frames. Misreading is treated as worse than missing throughout: ITF, Codabar, UPC-E, the GS1 DataBar family and
unchecked Code 39 results must repeat on a second scan line before they are reported, and a line beside it that reads
the same symbology differently - one clear of a spot the first line ran through - vetoes the read; guard patterns must
be adjacent rather than merely present, a Reed-Solomon correction is only accepted when the corrected block
re-verifies and never for more errors than its check codewords can locate (an Aztec block also only while it has
check codewords to spare for telling a repair from a miscorrection), and a Data Matrix grid must have a plausible
finder and timing pattern before its codewords are trusted.

Character sets: UTF-8, ISO-8859-1, UTF-16BE and ASCII are always available. QR Kanji and Hanzi segments and the
East Asian ECIs (Shift JIS, GB 2312, Big5, KS X 1001, GB 18030) decode to text when the application has registered
`CodePagesEncodingProvider` before the first scan; without it the text carries replacement characters for those
segments and `RawBytes` carries the exact bytes, so nothing is lost.

## Performance

### In the browser: compile ahead of time

Every stage of the decoder is C# working through pixels, and how the application is built decides what that costs
far more than anything else here. Blazor WebAssembly runs .NET on an IL interpreter by default, and on it those
loops are about a hundred times slower than compiled code. Measured in Chromium on a desktop, with the demo's own
pages built for Release:

| | Interpreted (Blazor's default) | Compiled ahead of time |
| --- | ---: | ---: |
| 640x480 camera frame, all formats, QR code in view | 68 ms | 0.8 ms |
| 640x480 camera frame, all formats, nothing in view | 104 ms | 1.0 ms |
| ... of which grayscale, binarization, matrix, linear | 1.5, 53, 50, 6 ms | 0.1, 0.2, 0.7, 0.1 ms |
| 1600x1200 photo of a QR code, picked from a file | 0.3 s | 0.1 s |
| 1600x1200 cluttered photo with no code in it | 10.8 s, of it one 9.3 s block | 0.6 s, at most 0.23 s at a time |

Interpreted, a desktop scans at about nine frames a second and the page stalls for a tenth of a second each time;
a phone is several times slower again. Compiled, the same frame uses well under a tenth of a 15 frames a second
budget. So an application that scans with the camera - above all one used on phones - should publish its client
compiled ahead of time:

```xml
<!-- The project that runs in the browser: the .Client project of a Blazor Web App, or a standalone app. -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <RunAOTCompilation>true</RunAOTCompilation>
</PropertyGroup>
```

It needs the `wasm-tools` workload (`dotnet workload install wasm-tools`), adds minutes to a publish, and makes the
download bigger: the demo goes from 1.9 MB to 4.2 MB, compressed. The demo is published that way.

Either way, the scanner keeps the browser's work to what only the browser can do and its own to what it must:

- **Grayscale on the GPU.** Where WebGL is available, the frame is read back through a shader that writes each
  pixel's luminance - four to a texel - so a frame crosses into managed memory as the one byte a pixel the decoder
  works on, a quarter of the RGBA frame, with nothing left to convert. A 2D canvas, the fallback, hands over RGBA.
- **Vectors where there are any.** The binarizer measures and thresholds two 8x8 blocks at a time with
  `Vector128`, which WebAssembly SIMD supports: binarizing a frame went from 0.8 to 0.2 ms compiled, and from about
  84 to 53 ms interpreted.
- **A quick look at a still image first.** When one symbol is wanted, a picked or dropped image is read at half size
  with the camera's settings before the exhaustive pass, and most photographs of a code are read by that look alone.
  When it finds nothing, the browser gets a turn - the "Reading image" notice is on screen - before the exhaustive
  pass, which is the long one interpreted.

The demo's performance page runs the frame benchmark in your own browser.

### In a MAUI app

With `Bit.BbScanner.Maui` a frame costs what it costs to copy the camera's luminance plane out of the platform's
buffer and scale it to `MaxProcessingSize` - a millisecond or two - and then what the decoder costs on .NET, measured
below. Nothing crosses the WebView, and the decode runs on a thread of its own rather than the app's UI thread, which
is where a BlazorWebView's components run. As everywhere, publish the app in Release: a Debug build of a MAUI app
runs on the interpreter, where a frame costs a hundred times more.

### Over a circuit, or a WebView

Under Blazor Server, and in a BlazorWebView without a camera of the platform's own, the decoder runs on .NET proper,
at the speeds measured below, and the cost that is new is getting the frame there. The page does what it can to make that small before it sends anything: the
GPU scales the frame and converts it to luminance, and the page cuts out the searched region, so a 640x360 frame is
230 KB searched in full and about 63 KB through `BbScanRegion.CenteredSquare(0.7)`. A tick is one call to ask what
changed and whether a frame is in, and the frame's slices when there is one; the loop asks for the next only
once the last is decoded, so a frame never waits in a queue, and a slow connection lowers the frame rate instead of
the freshness of what is decoded.

On a circuit, then, `Region` matters more than anywhere else - it is proportional to what crosses the network - and
`MaxProcessingSize` and `TargetFrameRate` are the controls for a slow link. On a LAN, or in a Hybrid app, where the
round trips are short, the defaults are fine.

### On .NET

Measured with BenchmarkDotNet on .NET 10, x64, 640x480 grayscale frames with the symbol centred: the decoder's own
cost, which is what an application compiled ahead of time, a server or a desktop application pays.

| Frame | All formats | QR only | Linear only |
| --- | ---: | ---: | ---: |
| QR, 29x29 modules | 297 us | 299 us | 290 us |
| QR, 57x57 modules | 373 us | 377 us | 801 us |
| QR with sensor noise | 362 us | 359 us | 7 735 us |
| Data Matrix | 550 us | 335 us | 315 us |
| PDF417, turned 8 degrees | 1 516 us | 349 us | 507 us |
| Aztec, turned 12 degrees | 964 us | 344 us | 398 us |
| Code 128 | 885 us | 369 us | 19 us |
| EAN-13 | 705 us | 342 us | 12 us |
| GS1 DataBar | 712 us | 344 us | 24 us |
| Empty frame | 607 us | 311 us | 45 us |

The "linear only" column on a frame that holds a matrix symbol is the cost of looking for something that is not
there at every orientation, which is the price of reading tilted symbols - and turned off sideways, which live
scanning has tried since it added two angles there, for about a third more on such a frame. It is only paid when no enabled symbology
matches, and narrowing `Formats` removes it. Matrix symbologies are tried in the order QR Code and Micro QR, Data
Matrix, PDF417, Aztec, MaxiCode, so a symbol of a later one also pays for the earlier ones looking first. Each symbology costs little on a
frame without it - Aztec about 30 us on an empty frame, 60 to 100 us on one holding other symbols and some 220 us on
one of pure noise. GS1 DataBar, the last added, costs nothing measurable on an empty frame or one holding another
linear symbol; the most it adds is on the noisy QR frame scanned for linear symbols only, where every line is a
long run of short bars, and there it was measured at 300 to 550 us of the 6 to 8 ms (`LinearButDataBar` in the
benchmarks).

Allocation per frame:

| Case | Allocated |
| --- | ---: |
| Linear only, no symbol found | **0 B** |
| QR only, empty frame | 40 B |
| All formats, empty frame | 88 B |
| Linear only, EAN-13 found | 504 B |
| All formats, QR found | 2 552 B |

A frame that finds nothing is the common case while a user is still aiming. It allocates nothing at all on the
linear path, and on the matrix path only the bit plane's wrapper object, whose buffer is pooled. Detectors, pattern
finders and their callbacks are all reused across frames rather than rebuilt, which is what keeps continuous
scanning free of collection pauses.

Where the time goes on a 640x480 frame:

| Stage | Cost |
| --- | ---: |
| Grayscale conversion from RGBA (skipped when the GPU hands over luminance) | 275 us |
| Grayscale with 2x subsampling | 113 us |
| Adaptive block binarization | 197 us |
| Binarizing one scan line | 1.0 us |
| QR detection and decode, symbol present | 83 us |
| Data Matrix detection, nothing present | 241 us |
| Code 128 on a row with no symbol | 1.5 us |
| EAN/UPC on a row with no symbol | 1.4 us |

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

- **One component, three ways to a camera.** The component reaches the camera through a bridge chosen for where it
  runs: `[JSImport]` on WebAssembly, `IJSRuntime` under Blazor Server and in a plain BlazorWebView, and an
  `IBbCameraSource` - the device's own camera - where the app registered one. Everything above the bridge - the frame
  loop, the lifecycle, the rendering - is one code path.
- **One copy per frame on WebAssembly.** `[JSImport]` with a `JSType.MemoryView` parameter hands JavaScript a view
  straight onto the managed buffer. No JSON, no base64, no intermediate array.
- **Less to send where frames travel.** Under Server and Hybrid the page sends the luminance of the searched region
  only, in slices a circuit message each, and everything the loop needs to know on a tick in one call.
- **The browser downsamples, and converts.** Frames are scaled to `MaxProcessingSize` by the GPU before they cross the
  boundary, and arrive as luminance, so the expensive stages never see pixels - or colour channels - they do not need.
- **Backpressure by construction.** The frame loop asks for the *latest* frame on a timer and gets nothing back when
  nothing new has arrived. A slow decode, or a slow connection, misses ticks rather than building a queue of stale
  frames.
- **Renders are rare.** Frames never enter the render tree.
- **Detection proposes, decoding disposes.** Both matrix detectors offer several candidate grids - with and without
  the alignment pattern, at neighbouring dimensions, from several patches of ink - and the first one that actually
  decodes wins. Committing to a single geometric guess is what makes a detector fail on symbols that are perfectly
  readable.
- **Failure is not exceptional.** Most frames contain no barcode, so every decode path reports failure by returning
  `null`, never by throwing.
- **One decoder everywhere, not the browser's.** The Barcode Detection API (`BarcodeDetector`) would hand decoding to
  the platform, but it is only there for some of the users an application has: in Chrome on Android, macOS and
  ChromeOS, not on Windows or Linux, behind a flag in Safari, and not in Firefox; and where it is, which formats it
  reads, how it treats check digits and misreads and what it reports as corners are the platform's. An engine that
  only some users get, with results that differ between them, would make every behaviour above conditional, so the
  library reads every symbol itself, the same way in every browser.

### Building blocks

The readers and binarizers are public building blocks rather than plug-ins: each implements `IBbRowDecoder` (one
binarized scan line in, a symbol out), `IBbMatrixDecoder` (a binarized image in) or `IBbBinarizer`, and can be
driven directly by a pipeline of your own - `new BbQrCodeReader().Decode(bits, BbMatrixDecodeOptions.Thorough)`.
`BbBarcodeDecoder` composes the built-in ones from `Formats` and does not accept others: the frame budget, the
scan-line geometry and the misread guards it applies are tuned to the symbologies it knows.

## PDF417 status

PDF417 is read end to end, by default: locating the symbol at any orientation, row detection, row indicator
interpretation, Reed-Solomon over GF(929), the text, byte and numeric compaction modes, ECI and the Macro PDF417
control block.

**The symbol character table.** It maps the 2 787 legal seventeen-module patterns to codeword values, and it is
specification data from ISO/IEC 15438 rather than a rule: of the 10 480 patterns that satisfy every structural
constraint, the three usable clusters hold 1 484, 1 002 and 1 002 members, and the specification lists which 929 of
each are used. The library ships that list as `BbPdf417SymbolTable.Specification`, reproduced from BWIPP (Barcode
Writer in Pure PostScript, MIT licensed) and checked entry for entry against the independent table in ZXing's
encoder; the tests check every entry against the structural rules again and read symbols BWIPP wrote.
`BbPdf417SymbolTable.Register` remains for an application that needs a table of its own; `BbBarcodeDecoder` leaves
PDF417 off while the registered table does not report `IsSpecificationTable`, rather than misread with it.

**Orientation.** A PDF417 row is three modules tall and often more than a hundred wide, so reading along rows of
pixels only works within about a degree of upright. A locator therefore runs first: scan lines at a handful of angles
find a start pattern, its eight-module bar is traced along its length to get the symbol's left edge and the direction
of its columns, a line along a row finds the stop pattern, whose bar is traced for the right edge, and the four
corners that come out of that are resampled upright from the grayscale image. The symbol is then read row by row as
if it had been printed straight. Every reference symbol reads at every orientation, in fifteen degree steps round the
full circle, at two and at four pixels a module, with the live camera settings.

**Compact PDF417.** Compact (truncated) PDF417, which drops the right row indicator and cuts the stop pattern to a
single bar, is read the same way: the left row indicators carry everything the symbol's structure needs. Rows are
read as a full symbol's first, and as a compact symbol's only when they do not make one, so a full symbol whose stop
pattern a line missed is never taken for a compact one.

**Not read.** MicroPDF417, which is a different symbology.

**Side by side.** A line along a row of one symbol runs on into the stop pattern of a symbol printed beside it, and
paired with that the quad comes out sheared. A stop pattern spans the same rows as its own start pattern, give or take
what perspective takes off one side, so the stop that is clearly best aligned with the start is the one used.

## Building and testing

```text
Bit.BbScanner                     the library
Bit.BbScanner.Maui                the device's own camera for .NET MAUI apps
Demo/Client                       the demo's pages, run on WebAssembly
Demo/Server                       the demo's host, and the MCP server
Samples/Core                      the samples' shared pages
Samples/Server, Wasm, Auto, Maui  the same pages in each hosting model
Tests/Bit.BbScanner.TestKit       encoders, image helpers and the sample catalog, shared by the tests, the benchmarks, the demo and the samples
Tests/Bit.BbScanner.Tests         unit, round-trip and image robustness tests
Tests/Bit.BbScanner.Tests.Mcp     the MCP server, over the wire
Tests/Bit.BbScanner.Tests.E2E     browser tests: a real page, a real camera stream, on WebAssembly and on a circuit
Tests/Bit.BbScanner.Benchmarks    BenchmarkDotNet suites
```

The encoders live in the test kit, not in the library: the library only reads barcodes. They exist so that every
decoder can be validated by round trip instead of by binary fixtures, and so that the demo can render real,
scannable symbols.

From `src/Utilities/BbScanner` (inside `src`, so `src/global.json` selects the Microsoft.Testing.Platform runner):

```bash
dotnet workload restore Samples/Maui/Bit.BbScanner.Samples.Maui.csproj  # once: the MAUI sample needs the MAUI workloads
dotnet build Bit.BbScanner.slnx
cd Tests/Bit.BbScanner.Tests && dotnet test
dotnet run --project Demo/Server
```

`Bit.BbScanner.slnx` includes the MAUI sample, whose Android, iOS and Mac Catalyst (and, on Windows, Windows) targets
need the `maui-*` workloads; without them the solution build fails with `NETSDK1147`. To work without them, build
the projects you need instead - `dotnet build Bit.BbScanner/Bit.BbScanner.csproj`, `dotnet test` from the tests
folder - or run a web sample: `dotnet run --project Samples/Server` (also `Samples/Wasm/Bit.BbScanner.Samples.Wasm`
and `Samples/Auto/Bit.BbScanner.Samples.Auto`). The MAUI sample itself runs with
`dotnet build -t:Run -f net10.0-android Samples/Maui`.

What the tests cover:

- Every symbology reads symbols written by an independent encoder - BWIPP, through bwip-js - as well as the test
  kit's own: clean, with the live camera settings in a 640x480 frame, and rotated, blurred and noisy. A mistake made
  identically by a test kit encoder and the matching decoder would pass every round trip; it cannot pass these. The
  fixtures are module grids and bar widths in `Tests/Bit.BbScanner.Tests/Reference`, next to the script that
  regenerates them; bwip-js is not a dependency of anything in the repository.
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

The component tests drive the Server and Hybrid paths end to end without a browser, against a fake of the page that
answers the module's calls and reads frames back, and a fake platform camera: the camera opening, regions cut out
and results reported in the frame's coordinates, a camera lost or closed by the watchdog, a native camera turned,
backgrounded and lost, frames decoded on another thread while the decoder is replaced, and the errors each reports.
`Bit.BbScanner.Maui` has been run on an Android emulator; on iOS, Mac Catalyst and Windows it is built, not yet
run by any test.

`Tests/Bit.BbScanner.Tests.E2E` runs the demo in Chromium whose camera is a generated video file, so the whole stack
is exercised: the WebAssembly runtime, the JavaScript module, a real `MediaStream`, the frame loop and the decoder.
It runs the Blazor Server sample the same way, on a real circuit, the watchdog included. Only the sensor is
synthetic. Its [README](Tests/Bit.BbScanner.Tests.E2E/README.md) says what it covers and how to run it.
