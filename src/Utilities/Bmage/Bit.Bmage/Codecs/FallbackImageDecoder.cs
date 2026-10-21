using Bit.Bmage.Memory;

namespace Bit.Bmage.Codecs;

/// <summary>
/// Decodes one format with a preferred decoder, and with a second one for the files the first rejects (corrupt for it,
/// or over the limits it enforces by refusing where the second scales down) or that <paramref name="prefersFallback"/>
/// sends there directly.
/// </summary>
internal sealed class FallbackImageDecoder(ImageFormat format, IImageDecoder preferred, IImageDecoder fallback, Func<ReadOnlyMemory<byte>, bool>? prefersFallback = null) : IImageDecoder
{
    public IReadOnlyCollection<ImageFormat> Formats { get; } = [format];

    public ImageInfo? Identify(ReadOnlySpan<byte> data) => preferred.Identify(data) ?? fallback.Identify(data);

    public async ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
    {
        if (prefersFallback?.Invoke(data) == true) return await fallback.DecodeAsync(data, options, allocator, cancellationToken).ConfigureAwait(false);
        try
        {
            return await preferred.DecodeAsync(data, options, allocator, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ImageDecodeException or ImageLimitExceededException)
        {
            return await fallback.DecodeAsync(data, options, allocator, cancellationToken).ConfigureAwait(false);
        }
    }
}
