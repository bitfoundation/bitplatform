namespace Bit.Bmage;

/// <summary>Decodes an encoded image into an <see cref="BmageImageBuffer"/>.</summary>
public interface IBmageImageDecoder
{
    /// <summary>The formats this decoder handles.</summary>
    IReadOnlyCollection<BmageImageFormat> Formats { get; }

    /// <summary>Reads header information without decoding pixels. Returns null if the data is not recognised.</summary>
    BmageImageInfo? Identify(ReadOnlySpan<byte> data);

    /// <summary>Decodes the image. The caller owns the returned buffer.</summary>
    ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default);
}
