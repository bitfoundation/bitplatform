using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage;

/// <summary>
/// Builds an <see cref="BmageImageProcessor"/> wired to whichever codecs this environment actually supports. Browser codecs
/// are registered on top of the managed ones so they take precedence where available.
/// </summary>
public sealed class BmageImageProcessorFactory
{
    private readonly BmageBrowserImageInterop _interop;
    private readonly IBmageImageCapabilityProvider _capabilities;
    private readonly BmageOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BmageImageProcessor? _processor;

    public BmageImageProcessorFactory(BmageBrowserImageInterop interop, IBmageImageCapabilityProvider capabilities, BmageOptions? options = null)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _options = options ?? new BmageOptions();
    }

    /// <summary>
    /// True when the file embeds a colour profile that is not sRGB, such as the Display P3 most phones now shoot in, or,
    /// for AVIF, signals wide gamut or HDR colours without one. The managed decoders read the stored values as sRGB, so
    /// those colours need the browser's colour management.
    /// </summary>
    internal static bool HasWideGamutProfile(BmageImageFormat format, ReadOnlyMemory<byte> data)
    {
        try
        {
            ReadOnlyMemory<byte> profile;
            if (format == BmageImageFormat.Avif)
            {
                // One parse, in place, for both the colours the file signals and the profile it embeds.
                if (BmageAvifDecoder.HasNonSrgbColors(data, out profile)) return true;
            }
            else
            {
                var metadata = format switch
                {
                    BmageImageFormat.Jpeg => BmageJpegContainer.ReadMetadata(data.Span),
                    BmageImageFormat.Png => BmagePngContainer.ReadMetadata(data.Span),
                    BmageImageFormat.WebP => BmageWebPContainer.ReadMetadata(data.Span),
                    _ => null,
                };
                profile = metadata?.IccProfile ?? default;
                // A PNG profile too large or too broken to inflate reads as none, yet it is no sRGB profile either.
                if (format == BmageImageFormat.Png && profile.IsEmpty && HasIccChunk(data.Span)) return true;
            }
            // Anything not recognised as sRGB goes to the browser, which costs bandwidth but never colour.
            return !profile.IsEmpty && !IsSrgbProfile(profile.Span);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }

    /// <summary>True when a PNG has an iCCP chunk before its image data, whether or not the profile in it could be read.</summary>
    private static bool HasIccChunk(ReadOnlySpan<byte> png)
    {
        foreach (var chunk in BmagePngContainer.EnumerateChunks(png))
        {
            if (chunk.Type == BmagePngContainer.MakeType("iCCP")) return true;
            if (chunk.Type == BmagePngContainer.MakeType("IDAT")) break;
        }
        return false;
    }

    /// <summary>
    /// The descriptions of the sRGB profiles cameras, phones and editors embed. A profile is recognised by its whole
    /// description: one that only mentions sRGB, such as a linear "sRGB-elle-V2-g10", describes other colours.
    /// </summary>
    private static readonly string[] SrgbProfileNames =
    [
        "sRGB IEC61966-2.1",
        "sRGB IEC61966-2-1 black scaled",
        "sRGB IEC61966-2-1 no black scaling",
        "sRGB v4 ICC preference perceptual intent beta",
        "sRGB2014",
        "sRGB built-in",
        "GIMP built-in sRGB",
        "sRGB",
    ];

    /// <summary>True when an ICC profile is an RGB profile whose description names it as one of the sRGB profiles.</summary>
    internal static bool IsSrgbProfile(ReadOnlySpan<byte> profile)
    {
        // A 128 byte header with the colour space at 16, then the tag count and 12 bytes per tag: signature, offset, size.
        if (profile.Length < 132 || !profile.Slice(16, 4).SequenceEqual("RGB "u8)) return false;
        var count = BinaryPrimitives.ReadUInt32BigEndian(profile[128..]);
        for (var i = 0; i < count && 132 + (i + 1) * 12 <= profile.Length; i++)
        {
            var entry = profile.Slice(132 + i * 12, 12);
            if (!entry[..4].SequenceEqual("desc"u8)) continue;
            var offset = BinaryPrimitives.ReadUInt32BigEndian(entry[4..]);
            var size = BinaryPrimitives.ReadUInt32BigEndian(entry[8..]);
            return offset <= (uint)profile.Length && size <= (uint)profile.Length - offset
                && DescribesSrgb(profile.Slice((int)offset, (int)size));
        }
        return false;
    }

    /// <summary>
    /// Reads a description tag: ASCII in ICC v2's textDescriptionType, UTF-16 in v4's multiLocalizedUnicodeType, where
    /// any of its translations may carry the name.
    /// </summary>
    private static bool DescribesSrgb(ReadOnlySpan<byte> tag)
    {
        if (tag.Length < 16) return false;
        if (tag[..4].SequenceEqual("desc"u8))
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(tag[8..]);
            return length <= (uint)(tag.Length - 12) && IsSrgbName(Encoding.ASCII.GetString(tag.Slice(12, (int)length)));
        }
        if (!tag[..4].SequenceEqual("mluc"u8)) return false;
        var records = BinaryPrimitives.ReadUInt32BigEndian(tag[8..]);
        var recordSize = BinaryPrimitives.ReadUInt32BigEndian(tag[12..]);
        if (recordSize < 12) return false;
        for (var i = 0L; i < records && 16 + (i + 1) * recordSize <= tag.Length; i++)
        {
            var record = tag.Slice((int)(16 + i * recordSize), 12);
            var length = BinaryPrimitives.ReadUInt32BigEndian(record[4..]);
            var offset = BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
            if (offset <= (uint)tag.Length && length <= (uint)tag.Length - offset
                && IsSrgbName(Encoding.BigEndianUnicode.GetString(tag.Slice((int)offset, (int)length))))
                return true;
        }
        return false;
    }

    private static bool IsSrgbName(string description) => Array.IndexOf(SrgbProfileNames, description.TrimEnd('\0', ' ')) >= 0;

    /// <summary>
    /// Returns the processor for this environment, creating it on first use. In a browser this prefers the native
    /// codecs; elsewhere it falls back to the managed ones so headless and prerendered code still works.
    /// </summary>
    public async ValueTask<BmageImageProcessor> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_processor is { } cached) return cached;
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_processor is { } existing) return existing;
            var capabilities = await _capabilities.GetAsync(cancellationToken).ConfigureAwait(false);
            var registry = BmageImageCodecRegistry.CreateDefault();
            var nativeDecodable = capabilities.NativeDecodableFormats ?? capabilities.DecodableFormats;
            var nativeEncodable = capabilities.NativeEncodableFormats ?? capabilities.EncodableFormats;
            if (capabilities.Environment == BmageImageEnvironment.BrowserWebAssembly)
            {
                var decodable = nativeDecodable.Where(f => f != BmageImageFormat.Svg).ToArray();
                var encodable = nativeEncodable.Where(f => !BmageImageCapabilities.ManagedOnlyEncodableFormats.Contains(f)).ToArray();
                if (decodable.Length > 0) registry.AddDecoder(new BmageBrowserImageDecoder(_interop, decodable));
                if (encodable.Length > 0) registry.AddEncoder(new BmageBrowserImageEncoder(_interop, encodable));
            }
            else if (capabilities.Environment == BmageImageEnvironment.BrowserServer)
            {
                // Over a circuit, every pixel a browser codec touches crosses the network: a 12 megapixel photo is
                // 48 MB of RGBA each way. The server's own codecs need only the file, so they handle every format they
                // can, and the browser only what they cannot: a file the managed decoder rejects or that is over the
                // limits, which the browser scales down to instead, and a photo with a colour profile other than sRGB,
                // which only the browser converts.
                var browser = new BmageBrowserImageDecoder(_interop, nativeDecodable.Where(f => f != BmageImageFormat.Svg));
                foreach (var format in browser.Formats)
                {
                    var managed = registry.GetDecoder(format);
                    registry.AddDecoder(managed is null
                        ? new BmageBrowserImageDecoder(_interop, [format])
                        : new BmageFallbackImageDecoder(format, managed, browser, data => HasWideGamutProfile(format, data)));
                }
                var encodable = nativeEncodable.Where(f => !BmageImageCapabilities.ManagedOnlyEncodableFormats.Contains(f) && !registry.CanEncode(f)).ToArray();
                if (encodable.Length > 0) registry.AddEncoder(new BmageBrowserImageEncoder(_interop, encodable));
            }
            var processor = new BmageImageProcessor(registry, _options.Allocator ?? BmagePooledPixelAllocator.Shared, _options.Limits) { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism };
            // Only cache once a real browser answered; a prerender pass must not pin the managed-only registry.
            if (capabilities.Environment != BmageImageEnvironment.Prerendering) _processor = processor;
            return processor;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Drops the cached processor so the next call re-detects capabilities.</summary>
    public void Invalidate() => _processor = null;
}
