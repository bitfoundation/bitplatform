using System.Drawing;
using System.Numerics;
using Bit.Bink;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Drawing;
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
    /// Encodes <paramref name="image"/> as a PNG for a Bink image element. Lossless, so nothing changes in the round trip;
    /// pass <paramref name="options"/> with <see cref="ImageExportOptions.MaxColors"/> for a smaller indexed PNG.
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
    /// layer named <see cref="ImageLayerName"/> and an empty layer above it to draw on.
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
        var layer = document.AddLayer(new BinkLayerProperties { Name = ImageLayerName }, index: 0);
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
    /// <exception cref="ArgumentException">The document has no fixed page, or the page is not the shape of <paramref name="size"/>.</exception>
    public static BinkRenderedImage RenderOverlay(this BinkDocument document, Size size, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (size.Width <= 0 || size.Height <= 0) throw new ArgumentOutOfRangeException(nameof(size), size, "The size must be positive.");
        var page = document.Settings.PageRect;
        if (page.Width <= 0 || page.Height <= 0) throw new ArgumentException("The document has no fixed page to fit to the image.", nameof(document));
        float scaleX = size.Width / page.Width, scaleY = size.Height / page.Height;
        if (Math.Abs(scaleX - scaleY) > 0.01f * Math.Max(scaleX, scaleY))
            throw new ArgumentException($"The page is {page.Width} x {page.Height}, not the shape of a {size.Width} x {size.Height} image.", nameof(size));

        var overlay = document.Clone();
        foreach (var layer in overlay.Layers.Where(IsImageLayer).ToList())
            overlay.UpdateLayer(layer.Id, p => p with { IsVisible = false });
        var rendered = overlay.RenderToImage((options ?? BinkPngExportOptions.Default) with
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
    public static ImageBuffer Flatten(ImageBuffer image, BinkDocument document, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var overlay = document.RenderOverlay(image.Size, options, allocator).Image;
        var result = image.Clone(allocator);
        try
        {
            using var canvas = ImageCanvas.FromBuffer(result);
            canvas.DrawImage(overlay, Vector2.Zero);
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
    public static ImageLayer ToImageLayer(this BinkDocument document, Size size, string? name = null, BinkPngExportOptions? options = null, IPixelAllocator? allocator = null)
    {
        var rendered = document.RenderOverlay(size, options, allocator);
        return new ImageLayer { Image = rendered.Image, Bounds = new RectangleF(0, 0, size.Width, size.Height), Name = name ?? "Drawing" };
    }

    private static bool IsImageLayer(BinkLayer layer) => layer.Properties.Name == ImageLayerName && layer.IsLocked;
}
