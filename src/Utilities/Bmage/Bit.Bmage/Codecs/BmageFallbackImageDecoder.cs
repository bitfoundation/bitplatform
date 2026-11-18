namespace Bit.Bmage;

/// <summary>
/// Decodes one format with a preferred decoder, and with a second one for the files the first rejects (corrupt for it,
/// or over the limits it enforces by refusing where the second scales down) or that <paramref name="prefersFallback"/>
/// sends there directly.
/// </summary>
internal sealed class BmageFallbackImageDecoder(BmageImageFormat format, IBmageImageDecoder preferred, IBmageImageDecoder fallback, Func<ReadOnlyMemory<byte>, bool>? prefersFallback = null) : IBmageImageDecoder
{
    public IReadOnlyCollection<BmageImageFormat> Formats { get; } = [format];

    public BmageImageInfo? Identify(ReadOnlySpan<byte> data) => preferred.Identify(data) ?? fallback.Identify(data);

    public async ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
    {
        if (prefersFallback?.Invoke(data) == true) return await fallback.DecodeAsync(data, options, allocator, cancellationToken).ConfigureAwait(false);
        try
        {
            return await preferred.DecodeAsync(data, options, allocator, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is BmageImageDecodeException or BmageImageLimitExceededException)
        {
            return await fallback.DecodeAsync(data, options, allocator, cancellationToken).ConfigureAwait(false);
        }
    }
}
