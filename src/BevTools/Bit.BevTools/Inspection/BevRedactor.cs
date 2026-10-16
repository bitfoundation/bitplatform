using System.Reflection;
using System.Text.RegularExpressions;

namespace Bit.BevTools;

/// <summary>Decides which member, header and query names are sensitive. Case-insensitive substring match plus <see cref="BevToolsSensitiveAttribute"/>.</summary>
public sealed class BevRedactor
{
    private static readonly Regex JwtPattern = new(@"ey[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]{6,}\.", RegexOptions.CultureInvariant);

    public const string RedactedValue = "«redacted»";

    private readonly string[] _patterns;

    public BevRedactor(IEnumerable<string> patterns)
    {
        _patterns = patterns.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToArray();
    }

    public static BevRedactor Default { get; } = new(new BitBevToolsOptions().SensitiveNamePatterns);

    public bool IsSensitiveName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (var pattern in _patterns)
        {
            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsSensitive(MemberInfo member)
    {
        if (member.IsDefined(typeof(BevToolsSensitiveAttribute), inherit: true))
        {
            return true;
        }

        return IsSensitiveName(member.Name);
    }

    /// <summary>Redacts embedded credentials and query-string values whose key looks sensitive; keeps everything else intact.</summary>
    public string RedactUrl(string url)
    {
        url = RedactUserInfo(url);
        var q = url.IndexOf('?');
        if (q < 0)
        {
            return url;
        }

        var parts = url[(q + 1)..].Split('&');
        var changed = false;
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=');
            var key = eq < 0 ? parts[i] : parts[i][..eq];
            var value = eq < 0 ? string.Empty : Uri.UnescapeDataString(parts[i][(eq + 1)..].Replace('+', ' '));
            if (IsSensitiveName(Uri.UnescapeDataString(key)) || JwtPattern.IsMatch(value) || value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = key + "=" + RedactedValue;
                changed = true;
            }
        }

        return changed ? url[..(q + 1)] + string.Join('&', parts) : url;
    }

    /// <summary>Removes credentials embedded in a URL (https://user:secret@host), which would otherwise be shown verbatim.</summary>
    private static string RedactUserInfo(string url)
    {
        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
        {
            return url;
        }

        var authorityStart = schemeEnd + 3;
        var authorityEnd = url.IndexOf('/', authorityStart);
        var authority = authorityEnd < 0 ? url[authorityStart..] : url[authorityStart..authorityEnd];
        var at = authority.LastIndexOf('@');
        return at < 0 ? url : url[..authorityStart] + RedactedValue + url[(authorityStart + at)..];
    }
}
