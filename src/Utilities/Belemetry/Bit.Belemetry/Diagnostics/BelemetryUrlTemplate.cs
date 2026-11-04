using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// Turns a URL or path into a template the diagnostic timeline can keep: the path segments that identify an instance
/// (numbers, GUIDs, long hexadecimal or mixed ids) become <c>{n}</c>, <c>{guid}</c>, <c>{hex}</c> or <c>{id}</c>, the
/// fragment goes, and only the allowlisted query parameters stay.
/// </summary>
internal static class BelemetryUrlTemplate
{
    /// <summary>
    /// The URL as <c>scheme://host[:port]/templated/path[?allowed=value]</c>, or the templated path of a relative URL;
    /// null for something that is not a URL.
    /// </summary>
    public static string? TemplateUrl(string? url, IReadOnlyCollection<string>? allowedQueryParameters)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var trimmed = url.TrimEnd('.', ',', ';', ':', '!', '?');
        string? origin = null;
        var rest = trimmed;

        var scheme = trimmed.IndexOf("://", StringComparison.Ordinal);
        if (scheme > 0)
        {
            var pathStart = trimmed.IndexOf('/', scheme + 3);
            origin = pathStart < 0 ? trimmed : trimmed[..pathStart];
            rest = pathStart < 0 ? "/" : trimmed[pathStart..];

            // Credentials in the authority ("https://user:pass@host") never stay.
            var at = origin.IndexOf('@', scheme + 3);
            if (at > 0) origin = origin[..(scheme + 3)] + origin[(at + 1)..];
        }

        var fragment = rest.IndexOf('#');
        if (fragment >= 0) rest = rest[..fragment];

        string? query = null;
        var question = rest.IndexOf('?');
        if (question >= 0)
        {
            query = rest[(question + 1)..];
            rest = rest[..question];
        }

        var builder = new StringBuilder(trimmed.Length);
        builder.Append(origin).Append(TemplatePath(rest));

        if (query is not null && allowedQueryParameters is { Count: > 0 })
        {
            var first = true;
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var equals = pair.IndexOf('=');
                var name = Uri.UnescapeDataString(equals < 0 ? pair : pair[..equals]);
                if (Contains(allowedQueryParameters, name) is false) continue;
                builder.Append(first ? '?' : '&').Append(pair);
                first = false;
            }
        }

        return builder.ToString();
    }

    /// <summary>The path with its instance segments replaced.</summary>
    public static string TemplatePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "/";

        var segments = path.Split('/');
        for (var i = 0; i < segments.Length; i++) segments[i] = TemplateSegment(segments[i]);
        return string.Join('/', segments);
    }

    private static string TemplateSegment(string segment)
    {
        if (segment.Length == 0 || segment[0] == '{') return segment;

        var digits = 0;
        var hex = 0;
        var letters = 0;
        foreach (var c in segment)
        {
            if (char.IsAsciiDigit(c)) digits++;
            if (char.IsAsciiHexDigit(c)) hex++;
            if (char.IsAsciiLetter(c)) letters++;
        }

        if (digits == segment.Length) return "{n}";
        if (segment.Length == 36 && Guid.TryParse(segment, out _)) return "{guid}";
        if (hex == segment.Length && segment.Length >= 16 && digits > 0) return "{hex}";

        // A long token mixing letters and digits (a slug with an id, a base64 key) identifies an instance; a word does not.
        if (segment.Length >= 16 && digits > 0 && letters > 0) return "{id}";

        // An e-mail address or anything the privacy rules scrubbed is a value, not a route.
        if (segment.Contains('@') || segment.Contains('[')) return "{value}";

        return segment;
    }

    private static bool Contains(IReadOnlyCollection<string> names, string name)
    {
        foreach (var candidate in names)
        {
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
