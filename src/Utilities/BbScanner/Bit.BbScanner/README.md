# Bit.BbScanner

A dependency-free, high performance barcode scanner for Blazor: QR Code, Micro QR, Data Matrix, PDF417, Aztec, MaxiCode,
Code 128, Code 39, Code 93, Codabar, EAN-13, EAN-8, UPC-A, UPC-E (with their add-ons), ITF, GS1 DataBar (with its stacked forms), DataBar
Limited and DataBar Expanded, from the camera or from still images. The decoding - luminance conversion, binarization,
detection, Reed-Solomon correction and bit stream parsing - is written in C#. JavaScript only opens the camera and
copies pixels - there is no barcode SDK or JavaScript scanner library underneath.

## Quick start

```bash
dotnet add package Bit.BbScanner
```

```razor
@* _Imports.razor *@
@using Bit.BbScanner
```

No services to register and no stylesheet to reference: the package's Blazor initializer injects
`_content/Bit.BbScanner/bit-bbscanner.css` automatically.

```razor
@rendermode InteractiveWebAssembly

<BbScanner Formats="BbBarcodeFormat.QrCode | BbBarcodeFormat.Ean13"
           Region="BbScanRegion.CenteredSquare(0.7)"
           OnScan="HandleScan"
           OnError="e => _error = e" />

@code {
    private BbScannerException? _error;

    private void HandleScan(BbBarcodeResult result)
        => Console.WriteLine($"{result.Format}: {result.Text}");
}
```

Camera scanning runs on Blazor WebAssembly - a standalone app, or the WebAssembly pages of a Blazor Web App - and
needs a secure context (HTTPS or `localhost`). Publish that client compiled ahead of time
(`<RunAOTCompilation>true</RunAOTCompilation>`): on Blazor's default interpreter a 640x480 frame took about 100 ms in
Chromium on a desktop, compiled about 1 ms. The decoder itself runs anywhere .NET runs:

```csharp
using var decoder = new BbBarcodeDecoder(BbScannerOptions.ForStillImages());
var result = decoder.Decode(rgbaPixels, width, height, BbPixelFormat.Rgba32);
```

## Features

- Eighteen symbologies read to specification, checked against symbols written by an independent encoder; PDF417,
  Aztec, MaxiCode and Micro QR at any orientation.
- One copy per frame: frames reach managed memory through a `[JSImport]` memory view, with no JSON or base64.
- No garbage per frame: buffers, bit planes and detectors are reused, so an empty frame allocates nothing on the
  linear path.
- Misreads guarded against: check digits, guard patterns and repeated reads are verified before a value is reported.
- Camera selection, torch and zoom; still image scanning through a built-in file picker or `DecodeImage`.
- Scan regions, render fragment overlays and message templates, and theming through CSS custom properties.
- A labelled, keyboard-operable scanner that announces results and alerts failures, every text translatable through
  `BbScannerTexts`, with right-to-left, reduced motion and forced colours support, and an optional vibration or beep.
- A camera the phone ends when the page goes into the background is reopened when it returns; `ReleaseCameraWhenHidden`
  closes it whenever the page is hidden.
- .NET 10 and 11.

## For AI agents

The demo site hosts an MCP server at `https://bbscanner.bitplatform.dev/mcp`
that answers from the shipped library: its API, its guide, its symbologies, a decode
lab and the demo's working pages.

## Documentation

The full guide - symbologies, the component's parameters, events and methods, still images, styling, the decoder
outside Blazor, hosting models, errors and permissions, what it reads, performance and architecture - is in the
[BbScanner README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BbScanner). See it running at
[bbscanner.bitplatform.dev](https://bbscanner.bitplatform.dev).
