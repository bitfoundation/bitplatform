namespace Bit.Bmage;

/// <summary>Encoded image formats known to Bmage.</summary>
public enum ImageFormat
{
    Unknown = 0,
    Jpeg,
    Png,
    WebP,
    Avif,
    Gif,
    Bmp,
    /// <summary>Scalable Vector Graphics. Only supported for rasterisation by browser decoders and treated as untrusted input.</summary>
    Svg,
    Tiff,
}

/// <summary>Helpers for <see cref="ImageFormat"/>: MIME types, extensions and magic-byte detection.</summary>
public static class ImageFormats
{
    /// <summary>Returns the canonical MIME type for the format, or <c>application/octet-stream</c> when unknown.</summary>
    public static string GetMimeType(this ImageFormat format) => format switch
    {
        ImageFormat.Jpeg => "image/jpeg",
        ImageFormat.Png => "image/png",
        ImageFormat.WebP => "image/webp",
        ImageFormat.Avif => "image/avif",
        ImageFormat.Gif => "image/gif",
        ImageFormat.Bmp => "image/bmp",
        ImageFormat.Svg => "image/svg+xml",
        ImageFormat.Tiff => "image/tiff",
        _ => "application/octet-stream",
    };

    /// <summary>Returns the default file extension (including the leading dot).</summary>
    public static string GetExtension(this ImageFormat format) => format switch
    {
        ImageFormat.Jpeg => ".jpg",
        ImageFormat.Png => ".png",
        ImageFormat.WebP => ".webp",
        ImageFormat.Avif => ".avif",
        ImageFormat.Gif => ".gif",
        ImageFormat.Bmp => ".bmp",
        ImageFormat.Svg => ".svg",
        ImageFormat.Tiff => ".tiff",
        _ => ".bin",
    };

    /// <summary>True for formats that can store an alpha channel.</summary>
    public static bool SupportsAlpha(this ImageFormat format) => format is ImageFormat.Png or ImageFormat.WebP or ImageFormat.Avif or ImageFormat.Gif or ImageFormat.Svg or ImageFormat.Tiff;

    /// <summary>True for formats that use lossy compression controlled by a quality setting.</summary>
    public static bool IsLossy(this ImageFormat format) => format is ImageFormat.Jpeg or ImageFormat.WebP or ImageFormat.Avif;

    /// <summary>True for formats that can carry EXIF metadata.</summary>
    public static bool SupportsExif(this ImageFormat format) => format is ImageFormat.Jpeg or ImageFormat.Png or ImageFormat.WebP or ImageFormat.Avif or ImageFormat.Tiff;

    /// <summary>Maps a MIME type (case-insensitive, parameters ignored) to a format.</summary>
    public static ImageFormat FromMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return ImageFormat.Unknown;
        var s = mimeType.AsSpan().Trim();
        var semi = s.IndexOf(';');
        if (semi >= 0) s = s[..semi].Trim();
        if (s.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) || s.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) || s.Equals("image/pjpeg", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Jpeg;
        if (s.Equals("image/png", StringComparison.OrdinalIgnoreCase) || s.Equals("image/apng", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Png;
        if (s.Equals("image/webp", StringComparison.OrdinalIgnoreCase)) return ImageFormat.WebP;
        if (s.Equals("image/avif", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Avif;
        if (s.Equals("image/gif", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Gif;
        if (s.Equals("image/bmp", StringComparison.OrdinalIgnoreCase) || s.Equals("image/x-ms-bmp", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Bmp;
        if (s.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Svg;
        if (s.Equals("image/tiff", StringComparison.OrdinalIgnoreCase)) return ImageFormat.Tiff;
        return ImageFormat.Unknown;
    }

    /// <summary>Maps a file name or extension to a format.</summary>
    public static ImageFormat FromFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return ImageFormat.Unknown;
        var ext = Path.GetExtension(fileName);
        if (ext.Length == 0) ext = fileName.StartsWith('.') ? fileName : "." + fileName;
        return ext.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".jpe" or ".jfif" => ImageFormat.Jpeg,
            ".png" or ".apng" => ImageFormat.Png,
            ".webp" => ImageFormat.WebP,
            ".avif" => ImageFormat.Avif,
            ".gif" => ImageFormat.Gif,
            ".bmp" or ".dib" => ImageFormat.Bmp,
            ".svg" => ImageFormat.Svg,
            ".tif" or ".tiff" => ImageFormat.Tiff,
            _ => ImageFormat.Unknown,
        };
    }

    /// <summary>
    /// Detects the format from the leading bytes of an encoded image (magic numbers). At least 12 bytes
    /// should be supplied for reliable detection; fewer bytes may yield <see cref="ImageFormat.Unknown"/>.
    /// </summary>
    public static ImageFormat Detect(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return ImageFormat.Jpeg;
        if (data.Length >= 8 && data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G' && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A) return ImageFormat.Png;
        if (data.Length >= 6 && data[0] == 'G' && data[1] == 'I' && data[2] == 'F' && data[3] == '8' && (data[4] == '7' || data[4] == '9') && data[5] == 'a') return ImageFormat.Gif;
        if (data.Length >= 12 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P') return ImageFormat.WebP;
        if (data.Length >= 2 && data[0] == 'B' && data[1] == 'M') return ImageFormat.Bmp;
        if (data.Length >= 4 && ((data[0] == 'I' && data[1] == 'I' && data[2] == 0x2A && data[3] == 0) || (data[0] == 'M' && data[1] == 'M' && data[2] == 0 && data[3] == 0x2A))) return ImageFormat.Tiff;
        if (data.Length >= 12 && data[4] == 'f' && data[5] == 't' && data[6] == 'y' && data[7] == 'p')
        {
            // ISO BMFF brand: avif / avis
            if (data[8] == 'a' && data[9] == 'v' && data[10] == 'i' && (data[11] == 'f' || data[11] == 's')) return ImageFormat.Avif;
            // Some encoders write a compatible-brand list; scan the box for an avif brand.
            var boxLen = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
            var limit = Math.Min(data.Length - 3, Math.Max(12, Math.Min(boxLen, 64)));
            for (var i = 16; i + 3 < limit; i += 4)
                if (data[i] == 'a' && data[i + 1] == 'v' && data[i + 2] == 'i' && (data[i + 3] == 'f' || data[i + 3] == 's')) return ImageFormat.Avif;
        }
        if (LooksLikeSvg(data)) return ImageFormat.Svg;
        return ImageFormat.Unknown;
    }

    private static bool LooksLikeSvg(ReadOnlySpan<byte> data)
    {
        // Skip UTF-8 BOM and whitespace, then expect '<' followed by "svg" or "?xml" or "!--" / "!DOCTYPE svg".
        var i = 0;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) i = 3;
        while (i < data.Length && (data[i] == ' ' || data[i] == '\t' || data[i] == '\r' || data[i] == '\n')) i++;
        if (i >= data.Length || data[i] != '<') return false;
        var head = data[i..Math.Min(data.Length, i + 512)];
        Span<char> chars = stackalloc char[head.Length];
        for (var k = 0; k < head.Length; k++) chars[k] = (char)head[k];
        var text = chars;
        return text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || ((text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) || text.StartsWith("<!", StringComparison.Ordinal)) && text.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
