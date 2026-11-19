namespace Bit.Bmage;

/// <summary>
/// A registry of decoders and encoders. Later registrations take precedence, so platform codecs (e.g. browser native)
/// registered after the managed defaults are preferred.
/// </summary>
public sealed class BmageImageCodecRegistry
{
    private readonly List<IBmageImageDecoder> _decoders = [];
    private readonly List<IBmageImageEncoder> _encoders = [];

    /// <summary>Creates a registry with the built-in managed codecs: PNG, JPEG, BMP, GIF, WebP and ICO, and AVIF decoding.</summary>
    public static BmageImageCodecRegistry CreateDefault()
    {
        var r = new BmageImageCodecRegistry();
        r.AddDecoder(new BmagePngDecoder());
        r.AddEncoder(new BmagePngEncoder());
        r.AddDecoder(new BmageBmpDecoder());
        r.AddEncoder(new BmageBmpEncoder());
        r.AddDecoder(new BmageJpegDecoder());
        r.AddEncoder(new BmageJpegEncoder());
        r.AddDecoder(new BmageGifDecoder());
        r.AddEncoder(new BmageGifEncoder());
        r.AddDecoder(new BmageIcoDecoder());
        r.AddEncoder(new BmageIcoEncoder());
        r.AddDecoder(new BmageWebPDecoder());
        r.AddEncoder(new BmageWebPEncoder());
        r.AddDecoder(new BmageAvifDecoder());
        return r;
    }

    public IReadOnlyList<IBmageImageDecoder> Decoders => _decoders;
    public IReadOnlyList<IBmageImageEncoder> Encoders => _encoders;

    public BmageImageCodecRegistry AddDecoder(IBmageImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        _decoders.Add(decoder);
        return this;
    }

    public BmageImageCodecRegistry AddEncoder(IBmageImageEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        _encoders.Add(encoder);
        return this;
    }

    /// <summary>Returns the preferred decoder for the format, or null.</summary>
    public IBmageImageDecoder? GetDecoder(BmageImageFormat format)
    {
        for (var i = _decoders.Count - 1; i >= 0; i--)
            if (_decoders[i].Formats.Contains(format)) return _decoders[i];
        return null;
    }

    /// <summary>Returns the preferred encoder for the format, or null.</summary>
    public IBmageImageEncoder? GetEncoder(BmageImageFormat format)
    {
        for (var i = _encoders.Count - 1; i >= 0; i--)
            if (_encoders[i].Formats.Contains(format)) return _encoders[i];
        return null;
    }

    public bool CanDecode(BmageImageFormat format) => GetDecoder(format) is not null;
    public bool CanEncode(BmageImageFormat format) => GetEncoder(format) is not null;

    /// <summary>Detects the format and reads header information. Returns null when unrecognised.</summary>
    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        var format = BmageImageFormats.Detect(data);
        if (format == BmageImageFormat.Unknown) return null;
        return GetDecoder(format)?.Identify(data);
    }
}
