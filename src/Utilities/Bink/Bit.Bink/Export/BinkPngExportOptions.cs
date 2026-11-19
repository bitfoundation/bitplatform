using System.IO.Compression;

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

    /// <summary>
    /// Maximum output pixels; export fails rather than allocating more. Guards against absurd scales. Embedded
    /// images are held to the same limit: each one, and all of them together, may decode to at most this many
    /// pixels, and an image that would go past it is skipped and reported in
    /// <see cref="BinkPngExportResult.SkippedImages"/> rather than failing the export.
    /// </summary>
    public long MaxPixels { get; init; } = 80_000_000;

    /// <summary>
    /// Maximum rasterizing work for <see cref="BinkPngExporter"/>, counted in edge tests, sorted crossings and pixels
    /// touched; past it the export throws <see cref="InvalidOperationException"/>. Unlimited by default. A server
    /// exporting documents it did not write sets it, since a document built to be expensive can otherwise keep one
    /// export busy for minutes; a page of handwriting at screen size needs well under a hundred million. An embedded
    /// image counts by the area it is drawn over, judged before it is decoded: one that would go past what is left of
    /// the budget fails the export even if its data turns out to be unreadable, while an unreadable one within it is
    /// skipped and reported in <see cref="BinkPngExportResult.SkippedImages"/>. A browser export does not rasterize in
    /// .NET and ignores it.
    /// </summary>
    public long MaxRasterWork { get; init; } = long.MaxValue;

    /// <summary>PNG compression level.</summary>
    public CompressionLevel Compression { get; init; } = CompressionLevel.Optimal;

    /// <summary>Physical resolution written into the file, in pixels per metre (0 omits it). 11811 is 300 DPI.</summary>
    public int PixelsPerMeter { get; init; }

    /// <summary>Optional text renderer; without one, text elements are not drawn by the .NET rasterizer.</summary>
    public IBinkTextRasterizer? TextRasterizer { get; init; }
}
