namespace Bit.Bink;

/// <summary>
/// An image referenced by a document. Images are stored as URIs so the document stays self-contained when
/// they are <c>data:</c> URIs, while large assets can be referenced by <c>https:</c> URL. Imported sources are
/// untrusted: only the <c>data:</c>, <c>http:</c>, <c>https:</c> and <c>blob:</c> schemes are accepted and the media type
/// of data URIs must be a raster image type (SVG must be explicitly allowed).
/// </summary>
public sealed record BinkImageSource
{
    /// <summary>Maximum accepted length of an image URI in characters (about 24 MB of base64), a defense against oversized documents.</summary>
    public const int MaxUriLength = 32 * 1024 * 1024;

    /// <summary>
    /// A source whose payload was left out of the document, which is how an embedded image is saved with
    /// <see cref="BinkSerializerOptions.IncludeEmbeddedImages"/> off. An element with this source keeps its place,
    /// crop, opacity and metadata but draws nothing until the application re-attaches the payload by replacing the
    /// element with one whose <see cref="BinkImageElement.Source"/> is the stored image.
    /// </summary>
    public static readonly BinkImageSource Omitted = new(string.Empty, 0, 0, null);

    private BinkImageSource(string uri, int width, int height, string? mediaType)
    {
        Uri = uri;
        Width = width;
        Height = height;
        MediaType = mediaType;
    }

    /// <summary>The image URI (<c>data:</c>, <c>https:</c>, <c>http:</c> or <c>blob:</c>).</summary>
    public string Uri { get; }

    /// <summary>Natural pixel width, or 0 when unknown.</summary>
    public int Width { get; }

    /// <summary>Natural pixel height, or 0 when unknown.</summary>
    public int Height { get; }

    /// <summary>Media type when known (e.g. <c>image/png</c>).</summary>
    public string? MediaType { get; }

    /// <summary>True for an embedded <c>data:</c> image.</summary>
    public bool IsEmbedded => Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the payload was left out of the document; see <see cref="Omitted"/>.</summary>
    public bool IsOmitted => Uri.Length == 0;

    /// <summary>Creates a source from encoded image bytes, embedding them as a data URI.</summary>
    /// <param name="bytes">Encoded image (PNG, JPEG, WebP, GIF, ...).</param>
    /// <param name="mediaType">Media type such as <c>image/png</c>.</param>
    /// <param name="width">Natural width in pixels.</param>
    /// <param name="height">Natural height in pixels.</param>
    public static BinkImageSource FromBytes(ReadOnlySpan<byte> bytes, string mediaType, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        if (!IsAllowedMediaType(mediaType, allowSvg: false)) throw new ArgumentException($"Unsupported image media type \"{mediaType}\". Use a raster type such as image/png or image/jpeg.", nameof(mediaType));
        string uri = "data:" + mediaType + ";base64," + Convert.ToBase64String(bytes);
        return new BinkImageSource(uri, Math.Max(width, 0), Math.Max(height, 0), mediaType);
    }

    /// <summary>Creates a source from a URI. Throws when the scheme is not allowed.</summary>
    /// <param name="uri">Image URI.</param>
    /// <param name="width">Natural width in pixels, or 0 if unknown.</param>
    /// <param name="height">Natural height in pixels, or 0 if unknown.</param>
    /// <param name="allowSvg">Whether <c>image/svg+xml</c> data URIs are accepted. SVG is drawn as an inert image but can still be large; enable only for trusted content.</param>
    public static BinkImageSource FromUri(string uri, int width = 0, int height = 0, bool allowSvg = false)
        => TryCreate(uri, width, height, allowSvg, out BinkImageSource? source)
            ? source!
            : throw new ArgumentException("The image URI must be a data:, https:, http: or blob: URI with a raster image media type.", nameof(uri));

    /// <summary>Tries to create a source from a URI, validating scheme, size and media type.</summary>
    public static bool TryCreate(string? uri, int width, int height, bool allowSvg, out BinkImageSource? source)
    {
        source = null;
        if (string.IsNullOrWhiteSpace(uri) || uri.Length > MaxUriLength) return false;
        uri = uri.Trim();
        string? mediaType = null;
        if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int comma = uri.IndexOf(',', StringComparison.Ordinal);
            if (comma < 0) return false;
            string header = uri[5..comma];
            int semi = header.IndexOf(';', StringComparison.Ordinal);
            mediaType = (semi < 0 ? header : header[..semi]).Trim().ToLowerInvariant();
            if (!IsAllowedMediaType(mediaType, allowSvg)) return false;
        }
        else if (!(uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || uri.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        if (uri.AsSpan().IndexOfAny("<>\"'") >= 0 || uri.AsSpan().IndexOfAny("\r\n\t ") >= 0) return false;
        source = new BinkImageSource(uri, Math.Max(width, 0), Math.Max(height, 0), mediaType);
        return true;
    }

    /// <summary>Returns a copy with known natural dimensions.</summary>
    public BinkImageSource WithSize(int width, int height) => new(Uri, Math.Max(width, 0), Math.Max(height, 0), MediaType);

    /// <summary>Decodes the bytes of an embedded data URI; returns false for remote images.</summary>
    public bool TryGetBytes(out byte[] bytes)
    {
        bytes = [];
        if (!IsEmbedded) return false;
        int comma = Uri.IndexOf(',', StringComparison.Ordinal);
        if (comma < 0 || !Uri.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            bytes = Convert.FromBase64String(Uri[(comma + 1)..]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsAllowedMediaType(string mediaType, bool allowSvg)
        => mediaType is "image/png" or "image/jpeg" or "image/jpg" or "image/webp" or "image/gif" or "image/bmp" or "image/avif"
           || (allowSvg && mediaType == "image/svg+xml");
}
