using System.Drawing;

namespace Bit.Bmage;

/// <summary>Basic facts about an encoded image obtained without a full decode.</summary>
public sealed record ImageInfo(int Width, int Height, BmageImageFormat Format, BmageOrientation Orientation = BmageOrientation.Normal, bool? HasAlpha = null)
{
    /// <summary>The size as it will be displayed once orientation is applied.</summary>
    public Size DisplaySize => Orientation.Transform(new Size(Width, Height));
    public string MimeType => Format.GetMimeType();
}

/// <summary>Options controlling decoding.</summary>
public sealed record DecodeOptions
{
    public static DecodeOptions Default { get; } = new();

    /// <summary>Safety limits. Decoders reject images exceeding them before allocating pixel memory.</summary>
    public ImageLimits Limits { get; init; } = ImageLimits.Default;

    /// <summary>Apply the EXIF orientation so pixels are upright (recommended). The stored orientation is then reset to normal.</summary>
    public bool AutoOrient { get; init; } = true;

    /// <summary>Read EXIF/ICC/XMP metadata into <see cref="ImageBuffer.Metadata"/>.</summary>
    public bool ReadMetadata { get; init; } = true;

    /// <summary>
    /// Optional hint: the decoder may produce an image no larger than this (keeping aspect ratio) when it can do so cheaply
    /// (e.g. browser decoders, JPEG DCT scaling). The result may still be larger; callers must resize if exactness matters.
    /// </summary>
    public Size? MaxSize { get; init; }

    /// <summary>File name of the source, stored in metadata for convenience.</summary>
    public string? FileName { get; init; }

    /// <summary>
    /// The most threads the managed decoders may spread the work after decoding over, such as turning a photo upright,
    /// or null for <see cref="OperationContext.DefaultMaxDegreeOfParallelism"/>. An
    /// <see cref="ImageProcessor"/> fills it in from its own <see cref="ImageProcessor.MaxDegreeOfParallelism"/>.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        init => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }
}

/// <summary>Options controlling encoding / export.</summary>
public sealed record ImageExportOptions
{
    public static ImageExportOptions Png { get; } = new() { Format = BmageImageFormat.Png };
    public static ImageExportOptions Jpeg { get; } = new() { Format = BmageImageFormat.Jpeg };
    public static ImageExportOptions WebP { get; } = new() { Format = BmageImageFormat.WebP };
    public static ImageExportOptions Avif { get; } = new() { Format = BmageImageFormat.Avif };

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

    /// <summary>Which metadata to keep. Defaults to <see cref="MetadataPolicy.Remove"/> for privacy and size.</summary>
    public MetadataPolicy Metadata { get; init; } = MetadataPolicy.Remove;

    /// <summary>Colour composited behind transparent pixels when the target format has no alpha channel. Default white.</summary>
    public Rgba32 Background { get; init; } = Rgba32.White;

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
    public MaxFileSizeStrategy MaxFileSizeStrategy { get; init; } = MaxFileSizeStrategy.QualityThenDimensions;

    /// <summary>Lowest quality the size search may use. Default 0.4.</summary>
    public double MinQuality { get; init; } = 0.4;

    /// <summary>Encoder specific settings that are not part of the common surface.</summary>
    public IReadOnlyDictionary<string, object>? Extensions { get; init; }
}

/// <summary>Strategy used to satisfy <see cref="ImageExportOptions.MaxFileSize"/>.</summary>
public enum MaxFileSizeStrategy
{
    /// <summary>Lower quality first (down to <see cref="ImageExportOptions.MinQuality"/>), then shrink dimensions.</summary>
    QualityThenDimensions = 0,
    /// <summary>Only lower quality; throw if the limit cannot be met.</summary>
    QualityOnly,
    /// <summary>Keep quality fixed and shrink dimensions only.</summary>
    DimensionsOnly,
}

/// <summary>The result of encoding an image.</summary>
public sealed class EncodedImage
{
    public EncodedImage(byte[] data, BmageImageFormat format, int width, int height, double? quality = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Format = format;
        Width = width;
        Height = height;
        Quality = quality;
    }

    /// <summary>The encoded bytes.</summary>
    public byte[] Data { get; }
    public BmageImageFormat Format { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>The quality actually used (may differ from the requested one when a size target was in effect).</summary>
    public double? Quality { get; }
    public string MimeType => Format.GetMimeType();
    public string Extension => Format.GetExtension();
    public long Length => Data.LongLength;

    /// <summary>Returns a data URL (base64). Use sparingly: this is expensive for large images.</summary>
    public string ToDataUrl() => $"data:{MimeType};base64,{Convert.ToBase64String(Data)}";

    public Stream AsStream() => new MemoryStream(Data, writable: false);
}

/// <summary>Decodes an encoded image into an <see cref="ImageBuffer"/>.</summary>
public interface IImageDecoder
{
    /// <summary>The formats this decoder handles.</summary>
    IReadOnlyCollection<BmageImageFormat> Formats { get; }

    /// <summary>Reads header information without decoding pixels. Returns null if the data is not recognised.</summary>
    ImageInfo? Identify(ReadOnlySpan<byte> data);

    /// <summary>Decodes the image. The caller owns the returned buffer.</summary>
    ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default);
}

/// <summary>Encodes an <see cref="ImageBuffer"/> to bytes.</summary>
public interface IImageEncoder
{
    /// <summary>The formats this encoder can produce.</summary>
    IReadOnlyCollection<BmageImageFormat> Formats { get; }

    /// <summary>Encodes the image. Metadata is written according to the options' policy.</summary>
    ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// A registry of decoders and encoders. Later registrations take precedence, so platform codecs (e.g. browser native)
/// registered after the managed defaults are preferred.
/// </summary>
public sealed class ImageCodecRegistry
{
    private readonly List<IImageDecoder> _decoders = [];
    private readonly List<IImageEncoder> _encoders = [];

    /// <summary>Creates a registry with the built-in managed codecs: PNG, JPEG, BMP, GIF, WebP and ICO, and AVIF decoding.</summary>
    public static ImageCodecRegistry CreateDefault()
    {
        var r = new ImageCodecRegistry();
        r.AddDecoder(new PngDecoder());
        r.AddEncoder(new PngEncoder());
        r.AddDecoder(new BmpDecoder());
        r.AddEncoder(new BmpEncoder());
        r.AddDecoder(new JpegDecoder());
        r.AddEncoder(new JpegEncoder());
        r.AddDecoder(new GifDecoder());
        r.AddEncoder(new GifEncoder());
        r.AddDecoder(new IcoDecoder());
        r.AddEncoder(new IcoEncoder());
        r.AddDecoder(new WebPDecoder());
        r.AddEncoder(new WebPEncoder());
        r.AddDecoder(new AvifDecoder());
        return r;
    }

    public IReadOnlyList<IImageDecoder> Decoders => _decoders;
    public IReadOnlyList<IImageEncoder> Encoders => _encoders;

    public ImageCodecRegistry AddDecoder(IImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        _decoders.Add(decoder);
        return this;
    }

    public ImageCodecRegistry AddEncoder(IImageEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        _encoders.Add(encoder);
        return this;
    }

    /// <summary>Returns the preferred decoder for the format, or null.</summary>
    public IImageDecoder? GetDecoder(BmageImageFormat format)
    {
        for (var i = _decoders.Count - 1; i >= 0; i--)
            if (_decoders[i].Formats.Contains(format)) return _decoders[i];
        return null;
    }

    /// <summary>Returns the preferred encoder for the format, or null.</summary>
    public IImageEncoder? GetEncoder(BmageImageFormat format)
    {
        for (var i = _encoders.Count - 1; i >= 0; i--)
            if (_encoders[i].Formats.Contains(format)) return _encoders[i];
        return null;
    }

    public bool CanDecode(BmageImageFormat format) => GetDecoder(format) is not null;
    public bool CanEncode(BmageImageFormat format) => GetEncoder(format) is not null;

    /// <summary>Detects the format and reads header information. Returns null when unrecognised.</summary>
    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        var format = ImageFormats.Detect(data);
        if (format == BmageImageFormat.Unknown) return null;
        return GetDecoder(format)?.Identify(data);
    }
}
