# Layers, masks and blend modes

A **mask** says where an edit applies. A **layer** adds something on top of the photo: pixels, a colour, or an edit of
everything beneath it. Both are immutable values, like operations and annotations, so they get undo and redo, proxy
previews and [JSON serialisation](#saving-and-replaying-an-edit) without any extra work.

## Selective edits

`Masked` applies any operation, or a whole sub-pipeline, only where a mask covers the image:

```csharp
// Darken and enrich the sky, fading out towards the horizon.
pipeline.Masked(Mask.Gradient(new(0, 0), new(0, 0.55f)), sky => sky.Exposure(-0.8f).Saturation(0.35f));

// Spotlight a subject: darken everything outside a soft ellipse.
var subject = new RadialGradientMask(new Vector2(0.3f, 0.55f), 0.22f, 0.38f) { Units = MaskUnits.Relative };
pipeline.Masked(subject.Invert(), around => around.Exposure(-1.2f));

// Recover blown highlights, and nothing else.
pipeline.Masked(new LuminanceRangeMask(0.8f, 1f), p => p.Exposure(-0.5f));
```

The contract is exact: where the mask is 0 the pixels are the original's, where it is 255 they are exactly what the
edit alone produces, and in between they mix by the coverage, in premultiplied alpha whenever the edit changed alpha.

It is also cheap. A run of adjustments through a mask is fused into one pass, as it is without a mask, and that pass
only visits the rows and columns the mask can reach: darkening the top third of a photo through a gradient costs a
third of darkening all of it, and the rows below are never loaded. The built-in masks are drawn only over that area too,
plus what their feather reaches into it, with exactly the values the whole mask has there. An edit that reads its
neighbours, such as a blur, runs over the whole image and is blended back by the mask.

The edit must keep the image's size. A crop or a resize has no pixel-for-pixel relationship with the original to blend
by, so `MaskedOperation` refuses it with an `ArgumentException` naming the operation.

## The masks

| Mask | Covers |
| --- | --- |
| `RectangleMask` | A rectangle, optionally with rounded corners. `Mask.All()` is the whole image. |
| `EllipseMask` | The ellipse inscribed in a rectangle. `Mask.Ellipse()` is the one inscribed in the whole image. |
| `PolygonMask` | A polygon through points: a lasso selection. |
| `PathMask` | Any `VectorPath`, including text outlines from `ITextRasterizer.GetTextPath`, for a knockout. |
| `BrushMask` | Brush strokes in the order they were painted; an erasing stroke removes what earlier ones painted. |
| `LinearGradientMask` | A graduated filter: fully covered up to its start, fading linearly to nothing at its end. |
| `RadialGradientMask` | A radial filter, rotatable, solid within its `Hardness` and fading smoothly to its edge. |
| `LuminanceRangeMask` | Pixels by brightness (Rec. 709), with a soft fall-off beyond the range. |
| `ColorRangeMask` | Pixels close to a colour, by "redmean" distance, which tracks perception far better than RGB distance. |
| `BitmapMask` | Another image's alpha or luminance, stretched over the photo: a segmentation model's matte. |
| `CombinedMask` | Two masks combined: `a.Union(b)`, `a.Intersect(b)`, `a.Subtract(b)`, `a.Exclude(b)`. |

Every mask has three modifiers:

| Property | Effect |
| --- | --- |
| `Feather` | Softens the edge with a gaussian of this standard deviation, in pixels. |
| `Inverted` | Swaps covered and uncovered. `mask.Invert()` returns the inverted copy. |
| `Opacity` | Multiplies the coverage, so an edit can be applied at partial strength. |

### Units

Geometric masks measure their coordinates in pixels of the image they apply to, or, with
`Units = MaskUnits.Relative`, in fractions of its width and height, so `(0, 0)` is the top left corner and `(1, 1)` the
bottom right. Relative masks fit every image, which is what a pipeline reused for uploads of any size needs. Pixel
masks rescale with the edit when it is previewed on a proxy; relative ones need not. `Feather` is always in pixels.

Content masks, `LuminanceRangeMask` and `ColorRangeMask`, select from the pixels the edit receives, so they need the
image: rendering one on its own with `Mask.Render(size)` and no source throws and says why.

### Rendering a mask yourself

`Mask.Render(size, source)` produces a `MaskBuffer`: one byte of coverage per pixel, a quarter of the memory of an
`ImageBuffer` and rented from the same allocator. It can also be made directly, painted on, combined and resized:

```csharp
using var matte = MaskBuffer.FromLuminance(segmentationOutput);   // or FromAlpha
using var full = matte.Resize(photo.Width, photo.Height);         // bilinear up, box down
full.Feather(2);
full.ApplyToAlpha(photo);                                          // cut the subject out
```

## Cut-outs

`ApplyMask` multiplies the image's alpha by a mask instead of restricting an edit to it:

```csharp
// A round avatar with transparent corners.
pipeline.Cover(512, 512).ApplyMask(Mask.Ellipse());

// A rounded card.
pipeline.ApplyMask(new RectangleMask(new RectangleF(0, 0, 1, 1)) { Units = MaskUnits.Relative, CornerRadius = 0.06f });

// Background removal: plug a model's matte in as a bitmap mask.
pipeline.ApplyMask(new BitmapMask(matte) { Channel = MaskChannel.Luminance, Feather = 1.5f });
```

Exporting to a format without alpha, such as JPEG, fills the cleared area with `ImageExportOptions.Background`.

## Layers

An `ImageDocument` composites its layers onto the pipeline's output, bottom first, and draws its annotations over
them:

```
ImageBuffer (original pixels, never modified)
        │
ImageDocument
   ├── ImagePipeline ── operations, some of them through masks
   ├── Layers        ── image, fill and adjustment layers, each with a blend mode, an opacity and a mask
   └── Annotations   ── arrows, text, redactions, drawn last
```

| Layer | What it adds |
| --- | --- |
| `ImageLayer` | Pixels drawn into `Bounds`, rotated about their centre, after a `Pipeline` of their own: a logo, a texture, a sticker. |
| `FillLayer` | A colour over the whole image, usually shaped by a mask: a colour wash, a gradient fade, a duotone. |
| `AdjustmentLayer` | An edit of everything beneath it. Unlike a pipeline step it also applies to the layers below, and it can be reordered, hidden or faded without touching anything else. |

```csharp
var document = ImageDocument.Create(photo)
    .AddOperation(new CropOperation(0, 0, 1600, 900))
    // A logo, blended into the photo.
    .AddLayer(new ImageLayer { Image = logo, Bounds = new RectangleF(1380, 680, 180, 180), BlendMode = BlendMode.Overlay })
    // A warm glow rising from the horizon.
    .AddLayer(new FillLayer(new Rgba32(255, 120, 40))
    {
        BlendMode = BlendMode.Screen,
        Opacity = 0.5f,
        Mask = Mask.Gradient(new(0, 0.75f), new(0, 0.3f)),
    })
    // More contrast in the shadows only.
    .AddLayer(new AdjustmentLayer(ImagePipeline.Create().Contrast(0.3f)) { Mask = new LuminanceRangeMask(0f, 0.35f) });

using var rendered = new DocumentRenderer().Render(document, photo);
```

Every layer has `Id`, `Name`, `BlendMode`, `Opacity`, `Mask`, `IsVisible` and `IsLocked`. Its coordinates, the mask's
included, are those of the pipeline's output. `ImageDocument` edits them like annotations: `AddLayer`, `UpdateLayer`
(by id), `RemoveLayer`, `MoveLayer` and `ClearLayers`.

An adjustment layer in the default `SourceOver` mode replaces what is below by its coverage, so at full opacity and
without a mask its result is exactly its pipeline's, and through a mask exactly `MaskedOperation`'s. In another mode it
blends the edited pixels onto the unedited ones: an adjustment layer with an empty pipeline in `Multiply` darkens the
image by itself, which is how photo editors build contrast curves out of blend modes.

An image layer runs its pipeline at the resolution its result is drawn at whenever that is smaller, the way a proxy
preview runs an edit, so a 24 megapixel texture placed in a corner costs what the corner does; a crop in the pipeline
counts, so a crop of a large logo is shrunk only as far as what the crop leaves. A layer's mask is drawn only over the
area the layer covers, so a small masked watermark on a large export costs its own pixels. In an `ImageEditSession` the
image a layer draws is prepared once and kept while the layer is faded, moved or re-blended, as long as its image,
pipeline and size stay the same.

### Without a document

`LayerCompositor` composites layers onto any buffer, or builds an image from layers alone:

```csharp
using var card = LayerCompositor.Flatten(new Size(1200, 630), Rgba32.White,
[
    new ImageLayer { Image = photo, Bounds = new RectangleF(0, 0, 1200, 630) },
    new FillLayer(new Rgba32(10, 20, 60)) { BlendMode = BlendMode.Multiply, Mask = Mask.Gradient(new(0, 1), new(0, 0.3f)) },
    new ImageLayer { Image = logo, Bounds = new RectangleF(40, 40, 160, 160), Opacity = 0.9f },
]);
```

### In the editor

`BmageEditor` previews and exports layers like every other edit: on the proxy while editing, at full resolution on
export. The preview keeps the composite while only annotations change, so moving an arrow over an expensive adjustment
layer does not composite the layers again. The panel's Layers tab lists them with show, hide and delete; custom chrome
drives them through the context:

```csharp
await editor.AddLayerAsync(new FillLayer(new Rgba32(40, 110, 220)) { BlendMode = BlendMode.Color, Opacity = 0.4f });
await editor.UpdateLayerAsync(layer with { Opacity = value }, transient: true);   // while a slider is dragged
await editor.MoveLayerAsync(layer.Id, 0);                                          // to the bottom
await editor.RemoveLayerAsync(layer.Id);
```

## Blend modes

`BlendMode` follows the W3C Compositing and Blending Level 1 specification, the same definitions as CSS
`mix-blend-mode` and the canvas `globalCompositeOperation`: the blend function decides the colour where the two
overlap, weighted by the destination's alpha, and the result is composited source-over. A transparent destination
therefore shows the source unchanged in every mode.

| Group | Modes |
| --- | --- |
| Compositing | `SourceOver` (normal), `Copy`, `Erase` |
| Darken | `Multiply`, `Darken`, `ColorBurn` |
| Lighten | `Screen`, `Lighten`, `ColorDodge`, `Add` |
| Contrast | `Overlay`, `HardLight`, `SoftLight` |
| Difference | `Difference`, `Exclusion` |
| Colour | `Hue`, `Saturation`, `Color`, `Luminosity` |

They apply to layers, to `ImageCanvas.DrawImage`, and to rows of pixels directly through `Compositor.Blend` and
`Compositor.BlendRow`.

## Saving and replaying an edit

`BmageJson` writes a document, pipeline, layers, masks and annotations included, as JSON, and reads it back as the same
edit:

```csharp
// In the browser: the edit, not the pixels, goes to the server.
var json = BmageJson.Serialize(editor.Document!, new BmageJsonOptions
{
    ImageReferences = image => uploads.IdOf(image),
});

// On the server: replay it on the original upload at full resolution.
var document = BmageJson.DeserializeDocument(json, new BmageJsonOptions { ImageResolver = id => uploads.Load(id) });
using var original = await processor.DecodeAsync(upload);
using var rendered = new DocumentRenderer(fonts).Render(document, original);

// Or reopen the editor where the user left off.
await editor.RestoreDocumentAsync(document);
```

The format is plain JSON with a `"type"` on every polymorphic object:

```json
{
  "format": "bmage-document",
  "version": 1,
  "sourceSize": [4000, 3000],
  "pipeline": [
    { "type": "masked",
      "operation": { "type": "exposure", "ev": -0.8 },
      "mask": { "type": "linearGradient", "feather": 12, "units": "Relative", "start": [0, 0], "end": [0, 0.55] } }
  ],
  "layers": [
    { "type": "fill", "id": "4b0f…", "blendMode": "Color", "opacity": 0.6, "color": "#286edcff" }
  ],
  "annotations": []
}
```

Every built-in operation, mask, layer and annotation round-trips exactly, and a test asserts that every public type has
a codec, so a new one cannot quietly be left out. The serialiser uses no reflection, which keeps it safe under trimming
and native AOT. Optional properties take their defaults when absent, so documents written by this version stay
readable by later ones, and malformed input is rejected with `ImageDocumentFormatException` naming where it went wrong,
such as `$.pipeline[2].rectangle`. A number JSON has no literal for, such as a rotation that came out as NaN, is written
as the string `"NaN"`, `"Infinity"` or `"-Infinity"` and read back as that number, so saving never fails on one.

A replayed document can come from anyone, so masks combined inside one another are capped: drawing a combination holds
its second side beside it, each the size of the image, and a document whose masks would hold more than eight at once is
refused. A chain built a step at a time, `a.Union(b).Union(c)`, holds one however long it is.

Pixels are never embedded: `ImageReferences` chooses what is written for an image an annotation, layer or mask refers
to, and `ImageResolver` supplies it on reading, so images can be stored beside the document and uploaded once. A custom
operation, layer, mask or annotation is written once it has a codec:

```csharp
var options = new BmageJsonOptions().Add<DuotoneOperation>("acme.duotone",
    (writer, duotone) => writer.Write("shadow", duotone.Shadow).Write("highlight", duotone.Highlight),
    reader => new DuotoneOperation(reader.GetColor("shadow"), reader.GetColor("highlight")));
```

Until then, serialising one throws `NotSupportedException` rather than silently dropping a step.
