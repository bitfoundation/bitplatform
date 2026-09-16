using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Sanitizes link and image URLs so untrusted Markdown cannot inject active
/// content (e.g. <c>javascript:</c> URIs) into the rendered output.
/// </summary>
internal static partial class BitMarkdownUrlSanitizer
{
    // All ASCII C0 control characters (0x00-0x1F) plus DEL (0x7F).
    [GeneratedRegex("[\u0000-\u001F\u007F]")]
    private static partial Regex ControlChars();

    // Schemes that are safe to allow for links.
    private static readonly string[] AllowedLinkSchemes =
        { "http:", "https:", "mailto:", "tel:", "ftp:", "ftps:" };

    // Schemes that are safe to allow for image sources.
    private static readonly string[] AllowedImageSchemes =
        { "http:", "https:" };

    // Inline image payloads that a browser only ever decodes as a bitmap. "image/svg+xml"
    // is deliberately absent: an SVG document can carry script, and although a browser does
    // not run it through <img>, allowing it would make the sanitizer's guarantee depend on
    // where the URL is used. Everything here is a raster format with no scripting model.
    private static readonly string[] AllowedImageDataTypes =
    {
        "data:image/png", "data:image/jpeg", "data:image/jpg", "data:image/gif",
        "data:image/webp", "data:image/avif", "data:image/bmp", "data:image/x-icon"
    };

    public static string Sanitize(string url, bool isImage)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        string trimmed = url.Trim();

        // Browsers treat leading backslashes like slashes when resolving URLs
        // (e.g. "\\evil.com" and "/\evil.com" behave like "//evil.com"), so reject
        // any URL whose leading run of separators contains a backslash.
        for (int i = 0; i < trimmed.Length && trimmed[i] is '/' or '\\'; i++)
        {
            if (trimmed[i] == '\\')
                return string.Empty;
        }

        // Relative URLs, anchors and protocol-relative URLs are allowed.
        if (trimmed.StartsWith('#') || trimmed.StartsWith('/') ||
            trimmed.StartsWith("./") || trimmed.StartsWith("../") ||
            trimmed.StartsWith("//"))
        {
            return EncodeSpaces(trimmed);
        }

        // Only treat the text before the first ':' as a scheme if it appears
        // before any '/', '?' or '#'. Otherwise it's a relative path.
        int colon = trimmed.IndexOf(':');
        if (colon < 0)
            return EncodeSpaces(trimmed); // no scheme => relative

        int slash = trimmed.IndexOfAny(new[] { '/', '?', '#' });
        if (slash >= 0 && slash < colon)
            return EncodeSpaces(trimmed); // ':' belongs to the path, not a scheme

        // Compare scheme case-insensitively, ignoring embedded control chars.
        // Browsers normalize away all ASCII C0 control characters (0x00-0x1F) and
        // DEL (0x7F) when resolving a URL, so strip them all to avoid scheme-based
        // XSS bypasses (per the WHATWG URL Standard).
        string scheme = ControlChars()
            .Replace(trimmed[..(colon + 1)], string.Empty)
            .ToLowerInvariant();
        var allowed = isImage ? AllowedImageSchemes : AllowedLinkSchemes;
        foreach (var s in allowed)
        {
            if (scheme == s)
                return EncodeSpaces(trimmed);
        }

        // Embedded raster images are a normal way to ship a self-contained document, so a
        // "data:image/png;base64,..." source is allowed for images (never for links, where
        // navigating to a data: URL is a known phishing vector).
        if (isImage && IsAllowedImageData(trimmed))
            return trimmed;

        // Unknown/blocked scheme.
        return string.Empty;
    }

    // A literal space is never valid in a URL - "[a](</my url>)" is written with one only
    // because the angle brackets let it be. Percent-encoding it keeps the attribute well
    // formed; every other character is left exactly as the author wrote it, so an already
    // encoded URL is never encoded twice.
    private static string EncodeSpaces(string url)
        => url.IndexOf(' ') < 0 ? url : url.Replace(" ", "%20");

    // Matches "data:<raster media type>" followed by the ';' or ',' that ends the type, so
    // "data:image/pngx" or a type with a trailing suffix cannot slip through on a prefix.
    private static bool IsAllowedImageData(string url)
    {
        foreach (var prefix in AllowedImageDataTypes)
        {
            if (url.Length <= prefix.Length) continue;
            if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is false) continue;
            if (url[prefix.Length] is ';' or ',') return true;
        }
        return false;
    }
}
