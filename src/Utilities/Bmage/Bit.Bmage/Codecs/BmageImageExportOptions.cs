using System.Drawing;

namespace Bit.Bmage;

/// <summary>Options controlling encoding / export.</summary>
public sealed record BmageImageExportOptions
{
    public static BmageImageExportOptions Png { get; } = new() { Format = BmageImageFormat.Png };
    public static BmageImageExportOptions Jpeg { get; } = new() { Format = BmageImageFormat.Jpeg };
    public static BmageImageExportOptions WebP { get; } = new() { Format = BmageImageFormat.WebP };
    public static BmageImageExportOptions Avif { get; } = new() { Format = BmageImageFormat.Avif };

    /// <summary>Target format. Defaults to PNG.</summary>
    public BmageImageFormat Format { get; init; } = BmageImageFormat.Png;

    /// <summary>Quality for lossy formats in [0,1]. Ignored by lossless formats. Default 0.85.</summary>
    public double Quality { get; init; } = 0.85;

    /// <summary>
    /// Encode losslessly with formats that can do either: WebP, which the managed encoder then writes wherever this runs,
    /// since no browser's canvas writes lossless WebP. In Blazor WebAssembly that is on the UI thread, which a large photo
    /// holds for seconds. <see cref="Quality"/> then does not apply, and a <see cref="MaxFileSize"/> is met by shrinking
    /// the dimensions. Other formats ignore it. Default false.
    /// </summary>
    public bool Lossless { get; init; }

    /// <summary>True when <see cref="Lossless"/> applies, that is, <see cref="Format"/> is one that can encode either way.</summary>
    internal bool EncodesLosslessly => Lossless && Format == BmageImageFormat.WebP;

    /// <summary>Which metadata to keep. Defaults to <see cref="BmageMetadataPolicy.Remove"/> for privacy and size.</summary>
    public BmageMetadataPolicy Metadata { get; init; } = BmageMetadataPolicy.Remove;

    /// <summary>Colour composited behind transparent pixels when the target format has no alpha channel. Default white.</summary>
    public BmageRgba32 Background { get; init; } = BmageRgba32.White;

    /// <summary>PNG compression effort: 0 (fastest) .. 9 (smallest). Default 6.</summary>
    public int PngCompressionLevel { get; init; } = 6;

    /// <summary>
    /// The most colours a palette-based file may hold, between 2 and 256, or null for none. A PNG is then written as an
    /// indexed image, often a third of the size of a full-colour one; an icon, logo or screenshot with no more colours
    /// than this keeps every pixel exactly, while a photo's colours are reduced to the palette, which changes them. A
    /// GIF always uses a palette, of 256 colours when this is null. Other formats ignore it. Default null.
    /// </summary>
    public int? MaxColors
    {
        get;
        init => field = value is < 2 or > 256 ? throw new ArgumentOutOfRangeException(nameof(MaxColors), value, "A palette holds between 2 and 256 colours.") : value;
    }

    /// <summary>
    /// When colours are reduced to a palette (<see cref="MaxColors"/>, or a GIF), spread each pixel's error to its
    /// neighbours, which hides banding in gradients at the cost of fine grain and a slightly larger file. Default true.
    /// </summary>
    public bool Dither { get; init; } = true;

    /// <summary>
    /// When set, the exporter searches for the highest quality (and, if needed, smaller dimensions) that keeps the file at
    /// or below this many bytes. Only meaningful for lossy formats; see <see cref="MaxFileSizeStrategy"/>.
    /// </summary>
    public long? MaxFileSize { get; init; }

    /// <summary>How to reach <see cref="MaxFileSize"/>.</summary>
    public BmageMaxFileSizeStrategy MaxFileSizeStrategy { get; init; } = BmageMaxFileSizeStrategy.QualityThenDimensions;

    /// <summary>Lowest quality the size search may use. Default 0.4.</summary>
    public double MinQuality { get; init; } = 0.4;

    /// <summary>Encoder specific settings that are not part of the common surface.</summary>
    public IReadOnlyDictionary<string, object>? Extensions { get; init; }
}
