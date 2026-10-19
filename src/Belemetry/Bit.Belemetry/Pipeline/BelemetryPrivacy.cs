using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Belemetry;

/// <summary>
/// The data-minimization rules of <see cref="BelemetryPrivacyOptions"/>: URL redaction and pattern scrubbing.
/// The same rules are implemented in <c>Scripts/privacy.ts</c>; keep both in step.
/// </summary>
internal sealed partial class BelemetryPrivacy(BelemetryPrivacyOptions options)
{
    internal const string Redacted = "REDACTED";

    private static readonly string[] _urlAttributes = [BelemetrySemanticConventions.UrlFull, "url.original", "page.referrer", "http.url", "http.target"];
    private static readonly string[] _textAttributes = [BelemetrySemanticConventions.ExceptionMessage, BelemetrySemanticConventions.ExceptionStacktrace, BelemetrySemanticConventions.UrlQuery];

    private readonly HashSet<string> _redactKeys = new(options.RedactQueryParameters, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _blocked = new(options.BlockedAttributes, StringComparer.Ordinal);

    public void Apply(BelemetrySpan span)
    {
        ApplyToAttributes(span.Attributes);
        if (span.StatusMessage is not null) span.StatusMessage = Scrub(span.StatusMessage);
        if (LooksLikeUrl(span.Name)) span.Name = RedactUrl(span.Name);
        foreach (var e in span.Events) ApplyToAttributes(e.Attributes);
    }

    public void Apply(BelemetryLogRecord log)
    {
        ApplyToAttributes(log.Attributes);
        if (log.Body is not null) log.Body = Scrub(log.Body);
    }

    public void ApplyToAttributes(BelemetryAttributes attributes)
    {
        if (_blocked.Count > 0) attributes.RemoveWhere((key, _) => _blocked.Contains(key));

        foreach (var key in _urlAttributes)
        {
            if (attributes.TryGetValue(key, out var value) && value is string url) attributes.Set(key, RedactUrl(url));
        }

        foreach (var key in _textAttributes)
        {
            if (attributes.TryGetValue(key, out var value) && value is string text) attributes.Set(key, Scrub(text));
        }
    }

    /// <summary>Redacts sensitive query parameters, strips the query or fragment as configured, then scrubs patterns.</summary>
    public string RedactUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        var fragmentIndex = url.IndexOf('#', StringComparison.Ordinal);
        var fragment = fragmentIndex >= 0 ? url[fragmentIndex..] : string.Empty;
        var withoutFragment = fragmentIndex >= 0 ? url[..fragmentIndex] : url;

        var queryIndex = withoutFragment.IndexOf('?', StringComparison.Ordinal);
        var path = queryIndex >= 0 ? withoutFragment[..queryIndex] : withoutFragment;
        var query = queryIndex >= 0 ? withoutFragment[(queryIndex + 1)..] : null;

        var builder = new StringBuilder(path);

        if (query is not null && options.StripQueryString is false)
        {
            builder.Append('?').Append(RedactQuery(query));
        }

        if (fragment.Length > 0 && options.StripFragment is false)
        {
            // OAuth implicit flows put tokens in the fragment as a query string (#access_token=...).
            builder.Append('#').Append(fragment.Contains('=', StringComparison.Ordinal) ? RedactQuery(fragment[1..]) : fragment[1..]);
        }

        return ScrubUrl(builder.ToString());
    }

    /// <summary>
    /// Scrubs a URL after its host: the host (an IP literal included) names the server that was called, not the user,
    /// and "http://[ip]:5000/" would hide which backend failed. Credentials in the authority are always dropped.
    /// </summary>
    private string ScrubUrl(string url)
    {
        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0) return Scrub(url);

        var authorityStart = schemeEnd + 3;
        var pathStart = url.IndexOfAny(['/', '?', '#'], authorityStart);
        if (pathStart < 0) pathStart = url.Length;

        var authority = url[authorityStart..pathStart];
        var at = authority.LastIndexOf('@');
        if (at >= 0) authority = authority[(at + 1)..];

        return string.Concat(url.AsSpan(0, authorityStart), authority, Scrub(url[pathStart..]));
    }

    private string RedactQuery(string query)
    {
        if (_redactKeys.Count == 0 || query.Length == 0) return query;

        var parts = query.Split('&');
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0) continue;

            var name = Uri.UnescapeDataString(parts[i][..eq].Replace('+', ' '));
            if (_redactKeys.Contains(name)) parts[i] = parts[i][..(eq + 1)] + Redacted;
        }

        return string.Join('&', parts);
    }

    /// <summary>Replaces e-mail addresses, JWTs, bearer tokens, Luhn-valid card numbers and IPv4 addresses.</summary>
    public string Scrub(string text)
    {
        if (options.ScrubPatterns is false || string.IsNullOrEmpty(text)) return text;

        text = EmailRegex().Replace(text, "[email]");
        text = JwtRegex().Replace(text, "[jwt]");
        text = BearerRegex().Replace(text, "Bearer [token]");
        text = CardRegex().Replace(text, m => IsLuhnValid(m.Value) ? "[card]" : m.Value);
        text = Ipv4Regex().Replace(text, "[ip]");
        return text;
    }

    internal static bool IsLuhnValid(string candidate)
    {
        var sum = 0;
        var digits = 0;
        var alternate = false;

        for (var i = candidate.Length - 1; i >= 0; i--)
        {
            var c = candidate[i];
            if (c is ' ' or '-') continue;
            if (c is < '0' or > '9') return false;

            var d = c - '0';
            if (alternate)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }

            sum += d;
            digits++;
            alternate = !alternate;
        }

        return digits is >= 13 and <= 19 && sum % 10 == 0;
    }

    private static bool LooksLikeUrl(string value) => value.Contains("://", StringComparison.Ordinal) || value.Contains('?', StringComparison.Ordinal);

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9._~+/\-]+=*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"\b(?:\d[ \-]?){12,18}\d\b", RegexOptions.CultureInvariant)]
    private static partial Regex CardRegex();

    // The lookbehind keeps assembly versions in .NET stack traces ("Version=10.0.0.0") from reading as addresses.
    [GeneratedRegex(@"(?<!Version=)\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b", RegexOptions.CultureInvariant)]
    private static partial Regex Ipv4Regex();
}
