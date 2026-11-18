using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>
/// Managed JPEG decoder supporting baseline and progressive Huffman-coded images, 8-bit precision, grayscale / YCbCr / RGB /
/// CMYK / YCCK colour, arbitrary chroma subsampling, restart intervals and EXIF orientation.
/// </summary>
public sealed class BmageJpegDecoder : IBmageImageDecoder
{
    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Jpeg];
    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (BmageImageFormats.Detect(data) != BmageImageFormat.Jpeg) return null;
        foreach (var seg in BmageJpegContainer.EnumerateSegments(data))
        {
            if (seg.Marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                if (seg.PayloadLength < 6) return null;
                var p = data.Slice(seg.PayloadOffset, seg.PayloadLength);
                var h = BinaryPrimitives.ReadUInt16BigEndian(p[1..]);
                var w = BinaryPrimitives.ReadUInt16BigEndian(p[3..]);
                var orientation = BmageJpegContainer.ReadOrientation(data);
                return w == 0 || h == 0 ? null : new BmageImageInfo(w, h, BmageImageFormat.Jpeg, orientation, HasAlpha: false);
            }
        }
        return null;
    }

    public ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static BmageImageBuffer Decode(ReadOnlySpan<byte> data, BmageDecodeOptions? options = null, IBmagePixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= BmageDecodeOptions.Default;
        allocator ??= BmagePooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        var core = new BmageJpegDecoderCore(data, options, cancellationToken);
        var image = core.Decode(allocator);
        try
        {
            if (options.ReadMetadata)
            {
                var md = BmageJpegContainer.ReadMetadata(data, options.FileName);
                image.Metadata = md;
                if (options.AutoOrient && md.Orientation is not (BmageOrientation.Normal or BmageOrientation.Unspecified))
                {
                    var oriented = new BmageOrientationOperation(md.Orientation).Apply(image, new BmageOperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true, maxDegreeOfParallelism: options.MaxDegreeOfParallelism));
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                    image.Metadata = md.WithOrientation(BmageOrientation.Normal);
                }
            }
            else image.Metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.Jpeg, fileName: options.FileName);
            return image;
        }
        catch { image.Dispose(); throw; }
    }
}
