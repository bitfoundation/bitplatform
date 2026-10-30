# Formats and capabilities

## What decodes and encodes where

| Format | Managed decode | Managed encode | Browser decode | Browser encode |
| --- | :---: | :---: | :---: | :---: |
| PNG | yes | yes | yes | yes |
| JPEG | yes | yes | yes | yes |
| BMP | yes | yes | yes | via managed |
| GIF | first frame | no | first frame | no |
| WebP | yes; first frame of an animation | lossy and lossless | yes | usually, lossy only |
| AVIF | yes; no colour management | no | usually | rarely |
| SVG | refused | no | refused | no |

"Managed" means pure C#, working anywhere including a server. "Browser" means the platform's own codecs, which is the
only way to write AVIF.

When both are available the browser codec wins, because it is hardware accelerated and covers more formats. Over a Blazor
Server circuit it is the other way round: the managed codecs handle every format they can on the server, because a
browser codec would send every pixel across the network, and the browser only decodes a file the managed decoder
rejects, or one whose colours need its colour management: a colour profile other than sRGB, or an AVIF that declares
wide gamut or HDR colours. The managed codecs are also the fallback that makes prerendering and server-side use work, and that
writes WebP in a browser whose canvas cannot, such as Safari's. Over a circuit, where that encoder runs on the server, the
capabilities report WebP as encodable everywhere. In Blazor WebAssembly it would run on the UI thread, many times slower
than the canvas, so there they report only what the canvas writes and `EncodeWithFallbackAsync` moves on to a format it
does; asking for WebP explicitly still works.

## Never assume, always probe

Format support varies by browser, version and platform, and browsers lie in a specific way: asked to encode a format
they cannot write, they silently produce a PNG instead. Bmage probes by actually encoding a pixel and checking
what came back.

```csharp
var capabilities = await capabilityProvider.GetAsync();

if (capabilities.CanEncode(ImageFormat.Avif)) { /* ... */ }
if (capabilities.SupportsWebP) { /* ... */ }
```

The substitution is detected rather than passed on:

```csharp
try
{
    var avif = await exporter.EncodeAsync(image, ImageExportOptions.Avif);
}
catch (ImageCapabilityException ex)
{
    // "This browser cannot encode Avif; it produced image/png instead. Check
    //  ImageCapabilities.CanEncode before exporting and fall back to a supported format."
}
```

If you would rather degrade than handle an exception, ask for a preference order:

```csharp
var encoded = await exporter.EncodeWithFallbackAsync(
    image,
    [ImageFormat.Avif, ImageFormat.WebP, ImageFormat.Jpeg],
    new ImageExportOptions { Quality = 0.82 });

Console.WriteLine(encoded.Format);   // whichever one this browser can actually write
```

## What else capability detection reports

```csharp
var c = await capabilityProvider.GetAsync();

c.Environment                  // BrowserWebAssembly, BrowserServer (Interactive Server, Blazor Hybrid), Server or Prerendering
c.DecodableFormats             // probed with real 1x1 images
c.EncodableFormats             // probed by really encoding
c.SupportsOffscreenCanvas
c.SupportsWebWorkers
c.SupportsImageBitmap
c.SupportsWebCodecs
c.SupportsClipboardRead / SupportsClipboardWrite
c.SupportsFileSystemAccess
c.SupportsCamera
c.SupportsWasmSimd / SupportsWasmThreads
c.DevicePixelRatio
c.HardwareConcurrency          // null when the browser does not say
c.DeviceMemoryGb               // null when the browser does not say
c.MaxCanvasDimension           // found by allocating until the browser refuses
c.SuggestedMaxWorkingDimension // a sensible interactive cap for this device
```

`MaxCanvasDimension` is worth taking seriously. Browsers do not expose their canvas limit, and exceeding it produces a
silently blank canvas rather than an error, which is far worse than a refusal. The probe allocates progressively smaller
canvases and writes a pixel to each until one reads back correctly. Chrome on a desktop typically reports 32767; Safari
on an older iPhone can be 4096.

```csharp
if (!capabilities.CanRenderCanvas(width, height))
{
    // Work at SuggestedMaxWorkingDimension instead of producing a blank image.
}
```

Detection is cached for the lifetime of the provider, because it cannot change without a page reload. It costs a few
milliseconds and runs once.

## Choosing a format

| Situation | Choose |
| --- | --- |
| Photographs, broad compatibility | JPEG at 0.8 to 0.85 |
| Photographs | WebP at 0.8, roughly 25 to 35% smaller than JPEG at similar quality |
| Anything needing transparency | WebP, lossy or lossless |
| An original that must keep every pixel | WebP with `Lossless = true`, usually smaller than PNG |
| Screenshots, diagrams, flat colour | PNG |
| Smallest possible, where supported | AVIF, but always with a fallback |
| Clipboard | PNG; most browsers accept nothing else |

Circular avatars need transparency, so prefer WebP or PNG there; a lossy opaque format will show the corners.

## Quality and size budgets

```csharp
var encoded = await exporter.EncodeAsync(image, new ImageExportOptions
{
    Format      = ImageFormat.WebP,
    Quality     = 0.9,
    MaxFileSize = 100 * 1024,
    MinQuality  = 0.5,
    MaxFileSizeStrategy = MaxFileSizeStrategy.QualityThenDimensions,
});

Console.WriteLine($"{encoded.Length} bytes at quality {encoded.Quality}");
```

`MaxFileSize` bisects on quality first, down to `MinQuality`, then shrinks dimensions if the strategy allows it. The
quality actually used is reported on the result, so you can show it. When the target is impossible you get an
`ImageEncodeException` explaining what the smallest attempt produced, rather than a silently oversized file.

| Strategy | Behaviour |
| --- | --- |
| `QualityThenDimensions` | Lower quality first, then shrink. The default. |
| `QualityOnly` | Never change dimensions; throw if the budget cannot be met. |
| `DimensionsOnly` | Keep quality fixed and shrink. |

## Transparency and formats without alpha

JPEG has no alpha channel. Exporting a transparent image as JPEG composites it over `ImageExportOptions.Background`,
which defaults to white. Set it explicitly if white is wrong for your page:

```csharp
new ImageExportOptions { Format = ImageFormat.Jpeg, Background = new Rgba32(18, 18, 20) }
```

Without this, browsers composite over black, which is rarely what anyone wants.

## Format detection

Content is trusted over file names and content types, because both can be wrong or hostile:

```csharp
var format = ImageFormats.Detect(bytes);              // magic bytes
var byName = ImageFormats.FromFileName("photo.JPG");  // convenience only
var byMime = ImageFormats.FromMimeType("image/webp"); // convenience only
```

`ImageProcessor.Identify` reads dimensions from the header without decoding pixels, which is how you can reject an
oversized upload before allocating anything:

```csharp
var info = processor.Identify(bytes);
if (info is null) return Error("Not a supported image.");
if ((long)info.Width * info.Height > 40_000_000) return Error("Too large.");
Console.WriteLine($"{info.Width}×{info.Height} {info.Format}, displays as {info.DisplaySize}");
```

`DisplaySize` accounts for EXIF orientation, so a portrait photo from a phone reports portrait dimensions even though
the stored pixels are landscape.

## WebP

The managed WebP codec reads every WebP: lossy and lossless, with or without alpha, the extended format's metadata, and
the first frame of an animation, which, as with GIF, is the only frame decoded. It writes lossy WebP by default, with
the alpha channel stored losslessly beside it when the image has transparency, and lossless WebP when asked:

```csharp
new ImageExportOptions { Format = ImageFormat.WebP, Quality = 0.8 }      // lossy
new ImageExportOptions { Format = ImageFormat.WebP, Lossless = true }    // lossless; Quality is ignored
```

A browser's canvas writes only lossy WebP, so a lossless export is always made by the managed encoder, wherever it
runs; in Blazor WebAssembly that is on the UI thread, which a large photo holds for seconds. WebP cannot hold an image wider or taller than 16383 pixels; encoding one throws `ImageEncodeException`.

## AVIF

The managed AVIF decoder reads the still images phones, browsers and encoders write: 8, 10 and 12 bits; 4:2:0, 4:2:2,
4:4:4 and monochrome; alpha, premultiplied or not; film grain; grids of tiles; rotation, mirroring and clean aperture
crops, which `AutoOrient` applies; EXIF, ICC and XMP; and the first frame of an image sequence. It implements the AV1
intra coding tools and filters, and its reconstruction matches libaom's and dav1d's exactly; only the final rounding to
8-bit RGB can differ by one.

What it does not do:

- **Colour management.** Pixels are converted from YUV with the matrix the file declares, but not from the file's
  primaries or transfer to sRGB, so a Display P3, BT.2020 or HDR (PQ, HLG) image decodes with muted colours. In a
  browser, and over a Blazor Server circuit, such files go to the browser's decoder, which converts them. The colours
  are read where the decoder reads them: the `nclx` colour box, or, when there is none or it leaves everything
  unspecified, the AV1 sequence header.
- **Layers predicted from one another.** Progressive and layered AVIFs whose upper layers are inter frames decode to
  their full-size base layer, or are refused when that layer is smaller.
- **Encoding.** AVIF is written only by a browser that can, which few can; always pair it with a fallback.

## GIF and SVG

GIF decodes its first frame, with palettes, interlacing and transparency. Animation is out of scope. GIF encoding is not
supported by any browser canvas and is not implemented here; use WebP or PNG.

SVG is refused with an explanatory error rather than rasterised. Rendering untrusted SVG can execute embedded scripts and
fetch remote resources, and a library cannot make that safe on the caller's behalf. If you control the SVG, rasterise it
yourself and pass the pixels.
