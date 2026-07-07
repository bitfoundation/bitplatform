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
            return trimmed;
        }

        // Only treat the text before the first ':' as a scheme if it appears
        // before any '/', '?' or '#'. Otherwise it's a relative path.
        int colon = trimmed.IndexOf(':');
        if (colon < 0)
            return trimmed; // no scheme => relative

        int slash = trimmed.IndexOfAny(new[] { '/', '?', '#' });
        if (slash >= 0 && slash < colon)
            return trimmed; // ':' belongs to the path, not a scheme

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
                return trimmed;
        }

        // Unknown/blocked scheme.
        return string.Empty;
    }
}
