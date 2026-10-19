using System.IO.Compression;
using System.Numerics;

namespace Bit.Bink;

/// <summary>Options for raster export.</summary>
public sealed record BinkPngExportOptions : BinkExportOptions
{
    /// <summary>Default options: one pixel per document unit.</summary>
    public static readonly BinkPngExportOptions Default = new();

    /// <summary>
    /// Output pixels per document unit. 2 or 3 produces a crisp image on high-DPI screens and in print. The
    /// result does not depend on the display the drawing was made on.
    /// </summary>
    public float Scale { get; init; } = 1f;

    /// <summary>Maximum output pixels; export fails rather than allocating more. Guards against absurd scales.</summary>
    public long MaxPixels { get; init; } = 80_000_000;

    /// <summary>PNG compression level.</summary>
    public CompressionLevel Compression { get; init; } = CompressionLevel.Optimal;

    /// <summary>Physical resolution written into the file, in pixels per metre (0 omits it). 11811 is 300 DPI.</summary>
    public int PixelsPerMeter { get; init; }

    /// <summary>Optional text renderer; without one, text elements are not drawn by the .NET rasterizer.</summary>
    public IBinkTextRasterizer? TextRasterizer { get; init; }
}

/// <summary>The outcome of a raster export.</summary>
/// <param name="Bytes">The encoded PNG.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="SkippedTextElements">Text elements that were not drawn because no text renderer was supplied.</param>
/// <param name="SkippedImages">Images that could not be decoded by the built-in decoder.</param>
public readonly record struct BinkPngExportResult(byte[] Bytes, int Width, int Height, int SkippedTextElements, int SkippedImages)
{
    /// <summary>True when everything in the document was drawn.</summary>
    public bool IsComplete => SkippedTextElements == 0 && SkippedImages == 0;
}

/// <summary>
/// Renders documents to PNG with the built-in .NET rasterizer. Works on a server, in tests and in WebAssembly
/// with no native dependencies, and produces byte-identical output for identical input.
/// </summary>
/// <remarks>
/// The rasterizer has no font engine and decodes only PNG images. For documents containing text or JPEG images,
/// either supply an <see cref="IBinkTextRasterizer"/> or use the browser-side export of the canvas component, which
/// draws with real fonts and the browser's image decoders. The result reports what was skipped rather than
/// silently producing an incomplete image.
/// </remarks>
public static class BinkPngExporter
{
    /// <summary>Renders a document to PNG bytes.</summary>
    public static byte[] Export(BinkDocument document, BinkPngExportOptions? options = null) => ExportWithDetails(document, options).Bytes;

    /// <summary>Renders a document to PNG and reports what could not be drawn.</summary>
    public static BinkPngExportResult ExportWithDetails(BinkDocument document, BinkPngExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= BinkPngExportOptions.Default;

        (BinkRasterSurface surface, BinkRasterRenderContext context) = Render(document, options);
        byte[] bytes = BinkPngCodec.Encode(surface.ToRgba32(), surface.Width, surface.Height, options.Compression, options.PixelsPerMeter);
        return new BinkPngExportResult(bytes, surface.Width, surface.Height, context.SkippedText, context.SkippedImages);
    }

    /// <summary>Renders a document into a raster surface without encoding it, for callers that want the pixels.</summary>
    public static BinkRasterSurface Render(BinkDocument document, BinkPngExportOptions? options, out int skippedText, out int skippedImages)
    {
        (BinkRasterSurface surface, BinkRasterRenderContext context) = Render(document, options ?? BinkPngExportOptions.Default);
        skippedText = context.SkippedText;
        skippedImages = context.SkippedImages;
        return surface;
    }

    private static (BinkRasterSurface Surface, BinkRasterRenderContext Context) Render(BinkDocument document, BinkPngExportOptions options)
    {
        BinkRect region = options.ResolveRegion(document);
        if (region.IsEmpty) region = new BinkRect(region.X, region.Y, MathF.Max(region.Width, 1f), MathF.Max(region.Height, 1f));

        float scale = options.Scale;
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(options), "The export scale must be a positive, finite number.");

        int width = Math.Max((int)MathF.Ceiling(region.Width * scale), 1);
        int height = Math.Max((int)MathF.Ceiling(region.Height * scale), 1);
        long pixels = (long)width * height;
        if (pixels > options.MaxPixels)
            throw new InvalidOperationException($"Exporting {width} by {height} pixels exceeds the limit of {options.MaxPixels} pixels. Lower the scale or export a smaller region.");

        var surface = new BinkRasterSurface(width, height);
        BinkColor background = options.Background ?? document.Settings.Background;
        surface.Clear(background);

        Matrix3x2 transform = Matrix3x2.CreateTranslation(-region.X, -region.Y) * Matrix3x2.CreateScale(scale);
        var context = new BinkRasterRenderContext(surface, transform) { TextRasterizer = options.TextRasterizer };

        var renderOptions = new BinkRenderOptions
        {
            ClipBounds = region,
            IncludeHiddenLayers = options.IncludeHiddenLayers,
            // An empty selection filters nothing, as in SvgExporter; otherwise the two exporters disagree on the
            // same options and the PNG comes out blank where the SVG has the whole drawing.
            ElementFilter = options.SelectedIds is { Count: > 0 } ? options.SelectedIds : null,
        };
        BinkDocumentRenderer.Render(document, context, renderOptions);
        return (surface, context);
    }
}
