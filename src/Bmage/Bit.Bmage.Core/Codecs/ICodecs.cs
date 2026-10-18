using System.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Codecs;

/// <summary>Basic facts about an encoded image obtained without a full decode.</summary>
public sealed record ImageInfo(int Width, int Height, ImageFormat Format, Orientation Orientation = Orientation.Normal, bool? HasAlpha = null)
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
}

/// <summary>Options controlling encoding / export.</summary>
public sealed record ImageExportOptions
{
    public static ImageExportOptions Png { get; } = new() { Format = ImageFormat.Png };
    public static ImageExportOptions Jpeg { get; } = new() { Format = ImageFormat.Jpeg };
    public static ImageExportOptions WebP { get; } = new() { Format = ImageFormat.WebP };
    public static ImageExportOptions Avif { get; } = new() { Format = ImageFormat.Avif };

    /// <summary>Target format. Defaults to PNG.</summary>
    public ImageFormat Format { get; init; } = ImageFormat.Png;

    /// <summary>Quality for lossy formats in [0,1]. Ignored by lossless formats. Default 0.85.</summary>
    public double Quality { get; init; } = 0.85;

    /// <summary>Which metadata to keep. Defaults to <see cref="MetadataPolicy.Remove"/> for privacy and size.</summary>
    public MetadataPolicy Metadata { get; init; } = MetadataPolicy.Remove;

    /// <summary>Colour composited behind transparent pixels when the target format has no alpha channel. Default white.</summary>
    public Rgba32 Background { get; init; } = Rgba32.White;

    /// <summary>PNG compression effort: 0 (fastest) .. 9 (smallest). Default 6.</summary>
    public int PngCompressionLevel { get; init; } = 6;

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
    public EncodedImage(byte[] data, ImageFormat format, int width, int height, double? quality = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Format = format;
        Width = width;
        Height = height;
        Quality = quality;
    }

    /// <summary>The encoded bytes.</summary>
    public byte[] Data { get; }
    public ImageFormat Format { get; }
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
    IReadOnlyCollection<ImageFormat> Formats { get; }

    /// <summary>Reads header information without decoding pixels. Returns null if the data is not recognised.</summary>
    ImageInfo? Identify(ReadOnlySpan<byte> data);

    /// <summary>Decodes the image. The caller owns the returned buffer.</summary>
    ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default);
}

/// <summary>Encodes an <see cref="ImageBuffer"/> to bytes.</summary>
public interface IImageEncoder
{
    /// <summary>The formats this encoder can produce.</summary>
    IReadOnlyCollection<ImageFormat> Formats { get; }

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

    /// <summary>Creates a registry with the built-in managed codecs (PNG, JPEG, BMP, GIF decode).</summary>
    public static ImageCodecRegistry CreateDefault()
    {
        var r = new ImageCodecRegistry();
        r.AddDecoder(new Png.PngDecoder());
        r.AddEncoder(new Png.PngEncoder());
        r.AddDecoder(new Bmp.BmpDecoder());
        r.AddEncoder(new Bmp.BmpEncoder());
        r.AddDecoder(new Jpeg.JpegDecoder());
        r.AddEncoder(new Jpeg.JpegEncoder());
        r.AddDecoder(new Gif.GifDecoder());
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
    public IImageDecoder? GetDecoder(ImageFormat format)
    {
        for (var i = _decoders.Count - 1; i >= 0; i--)
            if (_decoders[i].Formats.Contains(format)) return _decoders[i];
        return null;
    }

    /// <summary>Returns the preferred encoder for the format, or null.</summary>
    public IImageEncoder? GetEncoder(ImageFormat format)
    {
        for (var i = _encoders.Count - 1; i >= 0; i--)
            if (_encoders[i].Formats.Contains(format)) return _encoders[i];
        return null;
    }

    public bool CanDecode(ImageFormat format) => GetDecoder(format) is not null;
    public bool CanEncode(ImageFormat format) => GetEncoder(format) is not null;

    /// <summary>Detects the format and reads header information. Returns null when unrecognised.</summary>
    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        var format = ImageFormats.Detect(data);
        if (format == ImageFormat.Unknown) return null;
        return GetDecoder(format)?.Identify(data);
    }
}
