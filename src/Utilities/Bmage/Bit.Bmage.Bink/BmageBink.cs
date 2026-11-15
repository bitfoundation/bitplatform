using System.Drawing;
using System.Numerics;
using Bit.Bink;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Layers;
using Bit.Bmage.Memory;

namespace Bit.Bmage.Bink;

/// <summary>A Bink drawing rendered to pixels, with what the managed rasteriser had to leave out.</summary>
/// <param name="Image">The pixels. The caller owns them and disposes them.</param>
/// <param name="SkippedTextElements">
/// Text elements left out because no <see cref="BinkPngExportOptions.TextRasterizer"/> was given: Bink's managed
/// rasteriser draws text only through one.
/// </param>
/// <param name="SkippedImages">Image elements left out because they are not embedded PNG, which is all the managed rasteriser decodes.</param>
public sealed record BinkRenderedImage(ImageBuffer Image, int SkippedTextElements, int SkippedImages)
{
    /// <summary>True when every element was drawn.</summary>
    public bool IsComplete => SkippedTextElements == 0 && SkippedImages == 0;
}

/// <summary>
/// Moves images between Bmage and Bink: a Bmage image becomes a Bink document to draw on, and a Bink drawing becomes
/// Bmage pixels or a Bmage layer.
/// </summary>
/// <remarks>
/// <para>
/// The two libraries keep their own responsibilities. Bmage owns the raster image: decoding, transforms, adjustments,
/// export and metadata. Bink owns the drawing: strokes, shapes, text, ink and the interaction around them. The bridge
/// between them is a lossless PNG embedded in the document as a data URI, the one form Bink's managed rasteriser reads
/// back, so a document made here renders the same in the browser and on a server.
/// </para>
/// <para>
/// One document unit is one image pixel: <see cref="CreateDocument"/> sizes the page to the image, so a stroke drawn at
/// (120, 80) lands on the image's pixel (120, 80) when the drawing is rendered back.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var photo = await loader.LoadAsync(file);
/// var document = BmageBink.CreateDocument(photo);   // bind it to a BinkCanvas and let the user sign
/// // ...
/// using var signed = BmageBink.Flatten(photo, document);
/// var encoded = await processor.EncodeAsync(signed, ImageExportOptions.Jpeg);
/// </code>
/// </example>
public static class BmageBink
{
    /// <summary>The name of the layer <see cref="CreateDocument"/> puts the image on.</summary>
    public const string ImageLayerName = "Image";

    /// <summary>
    /// The key of the layer metadata entry that marks the layer <see cref="CreateDocument"/> puts the image on, which
    /// <see cref="RenderOverlay"/>, <see cref="Flatten"/> and <see cref="ToImageLayer"/> leave out. The mark is kept when
    /// the layer is renamed or unlocked, and is saved with the document; a layer the user names "Image" is drawn.
    /// </summary>
    public const string ImageLayerMetadataKey = "bit.bmage.layer";

    /// <summary>The value <see cref="ImageLayerMetadataKey"/> holds on the image's layer.</summary>
    public const string ImageLayerMetadataValue = "source-image";

    /// <summary>
    /// Encodes <paramref name="image"/> as a PNG for a Bink image element. Lossless, so nothing changes in the round trip;
    /// pass <paramref name="options"/> with <see cref="ImageExportOptions.MaxColors"/> for a smaller indexed PNG, which
    /// Bink's managed rasteriser reads at any of the 1, 2, 4 or 8 bits a pixel it is written with.
    /// </summary>
    /// <exception cref="ArgumentException">The PNG is larger than a Bink image source may be (32 MB as a data URI).</exception>
    public static BinkImageSource ToBinkImageSource(this ImageBuffer image, ImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        var png = PngEncoder.Encode(image, (options ?? ImageExportOptions.Png) with { Format = ImageFormat.Png });
        // Base64 grows the bytes by a third, and the URI's prefix adds a few more.
        if ((png.Length + 2) / 3 * 4 + 32 > BinkImageSource.MaxUriLength)
            throw new ArgumentException($"The image encodes to {png.Length} bytes of PNG, more than a Bink image source holds. Resize it first.", nameof(image));
        return BinkImageSource.FromBytes(png.Data, "image/png", image.Width, image.Height);
    }

    /// <summary>
    /// Creates a Bink document the size of <paramref name="image"/>, one unit a pixel, with the image on a locked bottom
    /// layer named <see cref="ImageLayerName"/>, marked with <see cref="ImageLayerMetadataKey"/>, and an empty layer above
    /// it to draw on.
    /// </summary>
    /// <param name="image">The image to draw on.</param>
    /// <param name="drawingLayerName">The name of the layer to draw on.</param>
    public static BinkDocument CreateDocument(ImageBuffer image, string drawingLayerName = "Drawing")
    {
        ArgumentNullException.ThrowIfNull(image);
        var document = new BinkDocument(BinkDocumentSettings.Default with { Width = image.Width, Height = image.Height, Background = BinkColor.Transparent });
        // The constructor's one layer becomes the drawing layer; the image goes beneath it.
        var drawing = document.Layers[0];
        document.UpdateLayer(drawing.Id, p => p with { Name = drawingLayerName });
        var layer = document.AddLayer(new BinkLayerProperties
        {
            Name = ImageLayerName,
            Metadata = new Dictionary<string, string> { [ImageLayerMetadataKey] = ImageLayerMetadataValue },
        }, index: 0);
        document.AddElement(new BinkImageElement(image.ToBinkImageSource(), new BinkRect(0, 0, image.Width, image.Height)) { IsLocked = true }, layer.Id);
        document.UpdateLayer(layer.Id, p => p with { IsLocked = true });
        return document;
    }

    /// <summary>
    /// Renders <paramref name="document"/> to a Bmage image through Bink's managed rasteriser, at
    /// <see cref="BinkPngExportOptions.Scale"/> pixels a unit.
    /// </summary>
    /// <param name="document">The drawing.</param>
    /// <param name="options">What to render and how; null for the page at one pixel a unit on the document's background.</param>
    /// <param name="allocator">Where the pixels come from; null for the shared pool.</param>
    public static BinkRenderedImage RenderToImage(this BinkDocument document, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var surface = BinkPngExporter.Render(document, options, out var skippedText, out var skippedImages);
        var image = ImageBuffer.FromRgba(surface.ToRgba32(), surface.Width, surface.Height, allocator);
        return new BinkRenderedImage(image, skippedText, skippedImages);
    }

    /// <summary>
    /// Renders what is drawn on <paramref name="document"/>, without the layer <see cref="CreateDocument"/> put the image
    /// on, onto transparency at <paramref name="size"/>: the page scaled to it, so a document made for an image lines up
    /// with that image or with a copy of it at another resolution.
    /// </summary>
    /// <remarks>
    /// Drawn on transparency, a layer or element that multiplies or screens has nothing to blend with: composite the
    /// overlay normally over the image and it covers it instead. <see cref="Flatten"/> draws on the image itself.
    /// </remarks>
    /// <exception cref="ArgumentException">The document has no fixed page, or the page is not the shape of <paramref name="size"/>.</exception>
    public static BinkRenderedImage RenderOverlay(this BinkDocument document, Size size, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var scaleX = PageScale(document, size);
        var rendered = WithoutImageLayer(document).RenderToImage((options ?? BinkPngExportOptions.Default) with
        {
            Region = BinkExportRegion.Page,
            Background = BinkColor.Transparent,
            Scale = scaleX,
            IncludeHiddenLayers = false,
        }, allocator);
        if (rendered.Image.Size == size) return rendered;
        // Both ways the scale is the same to within a pixel; what rounding added or left out is cropped or left clear.
        using var drawn = rendered.Image;
        var fitted = ImageBuffer.Create(size.Width, size.Height, allocator);
        var common = new Rectangle(0, 0, Math.Min(size.Width, drawn.Width), Math.Min(size.Height, drawn.Height));
        for (var y = 0; y < common.Height; y++) drawn.GetRow(y)[..common.Width].CopyTo(fitted.GetRow(y));
        return rendered with { Image = fitted };
    }

    /// <summary>
    /// A copy of <paramref name="image"/> with what is drawn on <paramref name="document"/> on top of it: the signed,
    /// marked up or annotated image, ready to export. The caller owns the result.
    /// </summary>
    /// <remarks>
    /// The drawing is rendered onto the image itself, so layers and elements that multiply or screen blend with its
    /// pixels as they do with the image layer when the whole document renders. The page is scaled to the image; the
    /// region, background and scale of <paramref name="options"/> are not used.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Something drawn could not be rendered: text without a <see cref="BinkPngExportOptions.TextRasterizer"/>, or an
    /// image that is not embedded PNG. <see cref="RenderOverlay"/> renders what it can and reports what it left out.
    /// </exception>
    public static ImageBuffer Flatten(ImageBuffer image, BinkDocument document, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(document);
        options ??= BinkPngExportOptions.Default;
        var scale = PageScale(document, image.Size);
        if ((long)image.Width * image.Height > options.MaxPixels)
            throw new InvalidOperationException($"Flattening {image.Width} by {image.Height} pixels exceeds the limit of {options.MaxPixels} pixels.");

        // The image, premultiplied as the rasteriser keeps its pixels, is what the drawing is rendered onto.
        var surface = new BinkRasterSurface(image.Width, image.Height);
        var target = surface.Pixels;
        for (var y = 0; y < image.Height; y++)
        {
            var row = image.GetRow(y);
            var o = y * image.Width * 4;
            for (var x = 0; x < row.Length; x++, o += 4)
            {
                var p = Premultiply(row[x]);
                target[o] = p.R; target[o + 1] = p.G; target[o + 2] = p.B; target[o + 3] = p.A;
            }
        }

        var page = document.Settings.PageRect;
        var context = new BinkRasterRenderContext(surface, Matrix3x2.CreateTranslation(-page.X, -page.Y) * Matrix3x2.CreateScale(scale))
        {
            TextRasterizer = options.TextRasterizer,
            MaxImagePixels = options.MaxPixels,
            ImagePixelBudget = options.MaxPixels,
            RasterWorkBudget = options.MaxRasterWork,
        };
        BinkDocumentRenderer.Render(WithoutImageLayer(document), context, new BinkRenderOptions
        {
            ClipBounds = page,
            ElementFilter = options.SelectedIds is { Count: > 0 } ? options.SelectedIds : null,
        });
        ThrowIfIncomplete(context.SkippedText, context.SkippedImages);

        var result = ImageBuffer.Create(image.Width, image.Height, allocator, clear: false);
        try
        {
            result.Metadata = image.Metadata;
            var drawn = surface.ToRgba32();
            for (var y = 0; y < image.Height; y++)
            {
                var source = image.GetRow(y);
                var row = result.GetRow(y);
                var o = y * image.Width * 4;
                for (var x = 0; x < row.Length; x++, o += 4)
                {
                    // A pixel the drawing left alone keeps its exact colour, which premultiplying would round where it is translucent.
                    var p = Premultiply(source[x]);
                    row[x] = target[o] == p.R && target[o + 1] == p.G && target[o + 2] == p.B && target[o + 3] == p.A
                        ? source[x]
                        : new Rgba32(drawn[o], drawn[o + 1], drawn[o + 2], drawn[o + 3]);
                }
            }
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// What is drawn on <paramref name="document"/> as a Bmage image layer covering an image of <paramref name="size"/>,
    /// so the drawing stays separate and editable in a Bmage document or the <c>BmageEditor</c>. The layer's image
    /// belongs to the caller, who disposes it once no document uses the layer.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Something drawn could not be rendered: text without a <see cref="BinkPngExportOptions.TextRasterizer"/>, or an
    /// image that is not embedded PNG. <see cref="RenderOverlay"/> renders what it can and reports what it left out.
    /// </exception>
    public static ImageLayer ToImageLayer(this BinkDocument document, Size size, string? name = null, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        var rendered = document.RenderOverlay(size, options, allocator);
        if (!rendered.IsComplete)
        {
            rendered.Image.Dispose();
            ThrowIfIncomplete(rendered.SkippedTextElements, rendered.SkippedImages);
        }
        return new ImageLayer { Image = rendered.Image, Bounds = new RectangleF(0, 0, size.Width, size.Height), Name = name ?? "Drawing" };
    }

    /// <summary>True for the layer <see cref="CreateDocument"/> put the image on, by its mark rather than its name.</summary>
    private static bool IsImageLayer(BinkLayer layer)
        => layer.Properties.Metadata?.TryGetValue(ImageLayerMetadataKey, out var value) == true && value == ImageLayerMetadataValue;

    /// <summary>A copy of the document with the image's layer hidden, or the document itself when it has none.</summary>
    private static BinkDocument WithoutImageLayer(BinkDocument document)
    {
        if (!document.Layers.Any(IsImageLayer)) return document;
        var overlay = document.Clone();
        foreach (var layer in overlay.Layers.Where(IsImageLayer).ToList())
            overlay.UpdateLayer(layer.Id, p => p with { IsVisible = false });
        return overlay;
    }

    /// <summary>Pixels a document unit for the page to cover <paramref name="size"/>, checked to be the page's shape.</summary>
    private static float PageScale(BinkDocument document, Size size)
    {
        if (size.Width <= 0 || size.Height <= 0) throw new ArgumentOutOfRangeException(nameof(size), size, "The size must be positive.");
        var page = document.Settings.PageRect;
        if (page.Width <= 0 || page.Height <= 0) throw new ArgumentException("The document has no fixed page to fit to the image.", nameof(document));
        float scaleX = size.Width / page.Width, scaleY = size.Height / page.Height;
        if (Math.Abs(scaleX - scaleY) > 0.01f * Math.Max(scaleX, scaleY))
            throw new ArgumentException($"The page is {page.Width} x {page.Height}, not the shape of a {size.Width} x {size.Height} image.", nameof(size));
        return scaleX;
    }

    /// <summary>Premultiplied as <see cref="BinkRasterSurface"/> stores a colour.</summary>
    private static Rgba32 Premultiply(Rgba32 p)
    {
        if (p.A == 255) return p;
        if (p.A == 0) return Rgba32.Transparent;
        var a = p.A / 255f;
        return new Rgba32((byte)MathF.Round(p.R * a), (byte)MathF.Round(p.G * a), (byte)MathF.Round(p.B * a), p.A);
    }

    private static void ThrowIfIncomplete(int skippedText, int skippedImages)
    {
        if (skippedText == 0 && skippedImages == 0) return;
        var missing = new List<string>(2);
        if (skippedText > 0) missing.Add($"{skippedText} text element{(skippedText == 1 ? "" : "s")}, which need a BinkPngExportOptions.TextRasterizer");
        if (skippedImages > 0) missing.Add($"{skippedImages} image{(skippedImages == 1 ? "" : "s")} that the managed rasteriser could not decode (it reads embedded PNG only)");
        throw new InvalidOperationException(
            $"The drawing could not be rendered whole: it leaves out {string.Join(" and ", missing)}. RenderOverlay renders what it can and reports what it left out.");
    }
}
