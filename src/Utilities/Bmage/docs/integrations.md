# Integrations with other bit utilities

Bmage owns raster images: decoding, transforms, adjustments, analysis, export and metadata. Two small packages connect
it to utilities that own something else, each adding a few calls and no new concepts. Neither is needed for anything
else in Bmage, and neither changes what `Bit.Bmage` itself depends on.

| Package | Connects | Depends on |
| --- | --- | --- |
| `Bit.Bmage.BbScanner` | Reading barcodes and QR codes from Bmage images with BbScanner's managed decoders | `Bit.Bmage`, `Bit.BbScanner` |
| `Bit.Bmage.Bink` | Drawing on Bmage images with Bink, and Bink drawings back into Bmage images and layers | `Bit.Bmage`, `Bit.Bink` |

## Barcodes and QR codes: Bit.Bmage.BbScanner

```bash
dotnet add package Bit.Bmage.BbScanner
```

A Bmage image is straight RGBA, the layout BbScanner calls `BbPixelFormat.Rgba32`, so its pixels go to the decoder as
they are, without a copy and without a canvas. It works in the browser and on a server alike.

```csharp
using Bit.BbScanner;          // BbBarcodeResult
using Bit.Bmage.BbScanner;

using var photo = await loader.LoadAsync(file);

// One attempt, the image as it is.
IReadOnlyList<BbBarcodeResult> codes = photo.Decode();

// As it is, then sharpened, then enlarged, until one finds a code.
var read = photo.ReadBarcodes();
if (read.Found)
{
    var code = read.Barcodes[0];
    Console.WriteLine($"{code.Format}: {code.Text}, read {read.Preprocessing}");
    var corners = read.CornersOf(code);   // in the photo's own pixels, whatever was enlarged
}
```

`ReadBarcodes` runs each preparation on a copy, so the image given is never changed, and stops at the first that finds a
code. Only preparation that was measured to turn failures into reads is offered:

| `BarcodePreprocessing` | What it does | What it rescues |
| --- | --- | --- |
| `None` | The image as it is | - |
| `Sharpen` | An unsharp mask | A code photographed slightly out of focus, QR or linear |
| `Enlarge` | Bicubic enlargement to `MinimumSize` (1200) pixels across; skipped for a larger image | A soft code only a few pixels a module |

Two other candidates were measured on BbScanner's own synthetic symbols, softened, small and unevenly lit, and left out
because they rescued nothing: thresholding against the surroundings (BbScanner already binarises locally, so it copes
with shadows), and enlarging and sharpening together (they read nothing that either alone did not). No rotation is
needed: the decoders find matrix codes at any angle and linear codes along rows and columns.

`BarcodeReadOptions` sets the decoders' own `BbScannerOptions` (default `BbScannerOptions.ForStillImages()`), the most
codes to return and which preparations to try, in order. A thresholded scan is the wrong input: read the codes from
the photo first, then clean the page up. The [document scanning](https://bmage.bitplatform.dev/scan) page does both.

## Drawing on images: Bit.Bmage.Bink

```bash
dotnet add package Bit.Bmage.Bink
```

Bink owns the drawing: strokes, shapes, text, ink and the interaction around them. The bridge is a lossless PNG
embedded in the Bink document, the one form Bink's managed rasteriser reads back, so a document made here renders the
same in a browser and on a server.

```csharp
using Bit.Bmage.Bink;

using var photo = await loader.LoadAsync(file);

// The image on a locked bottom layer named "Image", one document unit a pixel, and a layer above it to draw on.
var document = BmageBink.CreateDocument(photo);
// ... bind it to a BinkCanvas and let the user sign, mark up or annotate ...

// The drawing on the image, ready to export.
using var signed = BmageBink.Flatten(photo, document);

// Or the drawing as its own Bmage layer, which stays separate and editable.
var layer = document.ToImageLayer(photo.Size, "Signature");
var edited = imageDocument.AddLayer(layer);

// Or just the drawing on transparency, at any resolution of the same shape.
using var overlay = document.RenderOverlay(new Size(photo.Width * 2, photo.Height * 2)).Image;
```

| Call | Result |
| --- | --- |
| `image.ToBinkImageSource(options)` | A Bink image source: the image as an embedded PNG. Refused past Bink's 32 MB data URI limit. |
| `BmageBink.CreateDocument(image)` | A Bink document the size of the image with it on a locked bottom layer. |
| `document.RenderToImage(options)` | The document through Bink's managed rasteriser, as a Bmage image. |
| `document.RenderOverlay(size)` | What is drawn, without the image layer, on transparency, the page scaled to `size`. |
| `BmageBink.Flatten(image, document)` | A copy of the image with the drawing on top. |
| `document.ToImageLayer(size)` | The drawing as a Bmage `ImageLayer` covering the image. |

Each rendering reports, in `BinkRenderedImage`, what the managed rasteriser left out: text elements when no
`BinkPngExportOptions.TextRasterizer` was given, and image elements that are not embedded PNG. Check `IsComplete`, or
export text-heavy drawings through Bink's browser canvas instead. `Flatten` and `ToImageLayer` return only pixels, so
they throw `InvalidOperationException` rather than hand back a drawing with something missing.

`Flatten` renders the drawing onto the image itself, so a layer or a highlighter set to multiply or screen blends with
the photo as it does in the editor. `RenderOverlay` and `ToImageLayer` draw on transparency, where those modes have
nothing beneath them to blend with.

The image's layer is known by a mark in its metadata (`BmageBink.ImageLayerMetadataKey`), not by its name, so renaming
or unlocking it still leaves it out of an overlay, and a layer the user names "Image" is drawn like any other.

What is returned is yours: dispose the images, and an `ImageLayer`'s image once no document uses the layer.

## Other utilities

The rest of the bit utilities meet Bmage without a package of their own:

- **Borm** forms take an image upload as an `IBrowserFile`; process it with Bmage before the upload, as the
  [upload optimisation](https://bmage.bitplatform.dev/optimize) page shows, and register a custom renderer that hosts a
  `BmageEditor` with `Tools` and `Panels` set for a cropping field (see [the editor](editor.md#a-cropper)).
- **BlazeDb, Bsqlite and Bcrdt** store what an edit is: `BmageJson` serialises a document to a string and a `.bmage`
  project to a ZIP of the JSON and its images, both plain data a database stores and a CRDT references by version.
  Keep pixels out of replicated state; store the original once and the document beside it.
- **Blex** holds editor state such as the selected tool or the document's JSON; pixel buffers belong in the session,
  not in a store.
- **Bdnd** and **Bortable** move files and order lists; `BmageDropZone` and the editor's own drop handling take the
  files they hand over, and a layer list is reordered with `ImageDocument.MoveLayer`.
