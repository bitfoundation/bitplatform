using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Belemetry;

/// <summary>
/// The data-minimization rules of <see cref="BelemetryPrivacyOptions"/>: blocked attributes, URL redaction, and
/// pattern scrubbing of every string attribute, span status and log body. The same rules are implemented in
/// <c>Scripts/privacy.ts</c>; keep both in step (a relayed browser record goes through both).
/// </summary>
internal sealed partial class BelemetryPrivacy(BelemetryPrivacyOptions options)
{
    internal const string Redacted = "REDACTED";

    // Attributes that hold URLs: their query parameters and fragment are redacted before scrubbing.
    private static readonly HashSet<string> _urlAttributes = new([BelemetrySemanticConventions.UrlFull, "url.original", "page.referrer", "http.url", "http.target"], StringComparer.Ordinal);

    // Untouched by every rule but BlockedAttributes, as in the script: the ids the enrichment step adds (and the browser
    // adds before relaying) are governed by consent - an app that sets an e-mail address as the user id with
    // UserTracking consent meant to. Blocking one is the operator's explicit choice, and wins.
    private static readonly HashSet<string> _idAttributes = new(
        [BelemetrySemanticConventions.SessionId, BelemetrySemanticConventions.EndUserPseudoId, BelemetrySemanticConventions.UserId], StringComparer.Ordinal);

    // Not pattern-scrubbed (an explicit RedactedAttributes name still applies): the server that was called, not the
    // user; and the user agent, whose four-part versions ("Chrome/120.0.0.0") read as IPv4 addresses.
    private static readonly HashSet<string> _hostAttributes = new(
        [BelemetrySemanticConventions.ServerAddress, BelemetrySemanticConventions.UserAgentOriginal], StringComparer.Ordinal);

    // Attribute names are few and repeat in every record: the verdict per key is cached, up to a bound past which a
    // misbehaving app (a key per user, say) costs a lookup instead of memory.
    private const int MaxCachedKeys = 1024;
    private int _cachedKeys;

    private readonly HashSet<string> _redactKeys = new(options.RedactQueryParameters, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _blocked = new(options.BlockedAttributes, StringComparer.Ordinal);
    private readonly HashSet<string> _redactedNames = new(options.RedactedAttributes.Where(n => n.EndsWith('*') is false), StringComparer.OrdinalIgnoreCase);
    private readonly string[] _redactedPrefixes = [.. options.RedactedAttributes.Where(n => n.EndsWith('*')).Select(n => n[..^1])];
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, KeyRule> _rules = new(StringComparer.Ordinal);

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

    /// <summary>Whether <paramref name="key"/> is one of <see cref="BelemetryPrivacyOptions.BlockedAttributes"/>: the enrichment step does not add a blocked id.</summary>
    public bool IsBlocked(string key) => _blocked.Count > 0 && _blocked.Contains(key);

    /// <summary>
    /// Removes the blocked attributes, replaces the values of the redacted names, redacts the URL attributes and
    /// scrubs every other string value - a structured log value or an event attribute can carry the same e-mail
    /// address the message does. In the script's order: a blocked key goes, an id included; the ids are left alone by
    /// every other rule, <c>server.address</c> and <c>user_agent.original</c> are not pattern-scrubbed, and a key
    /// named like a version keeps its IPv4-looking values (<c>{AppVersion} = "1.2.0.0"</c>).
    /// </summary>
    public void ApplyToAttributes(BelemetryAttributes attributes)
    {
        if (_blocked.Count > 0) attributes.RemoveWhere((key, _) => _blocked.Contains(key));

        if (options.StripQueryString) attributes.Remove(BelemetrySemanticConventions.UrlQuery);

        List<KeyValuePair<string, object>>? changed = null;
        foreach (var (key, value) in attributes)
        {
            var rule = Rule(key);
            object next;
            switch (rule)
            {
                case KeyRule.Id or KeyRule.Host:
                    continue;
                case KeyRule.Redacted:
                    next = Redacted;
                    break;
                case KeyRule.Url when value is string url:
                    next = RedactUrl(url);
                    break;
                case KeyRule.Query when value is string query:
                    // The convention leaves the '?' out, but not every instrumentation does.
                    next = Scrub(query.StartsWith('?') ? "?" + RedactQuery(query[1..]) : RedactQuery(query));
                    break;
                default:
                    if (value is string text) next = Scrub(text, addresses: rule != KeyRule.Version);
                    else if (value is string[] texts && options.ScrubPatterns) next = ScrubAll(texts, addresses: rule != KeyRule.Version);
                    else continue;
                    break;
            }

            // Collected and set afterwards: the attributes cannot change while they are being enumerated. A URL is
            // rebuilt even when nothing in it was redacted, so strings compare by value.
            var same = next is string s ? string.Equals(s, value as string, StringComparison.Ordinal) : ReferenceEquals(next, value);
            if (same is false) (changed ??= []).Add(new(key, next));
        }

        if (changed is null) return;
        foreach (var (key, value) in changed) attributes.Set(key, value);
    }

    /// <summary>Which rule an attribute key falls under, in the order they apply; the first that matches wins.</summary>
    private enum KeyRule : byte
    {
        None,
        Id,
        Redacted,
        Host,
        Url,
        Query,
        Version,
    }

    /// <summary>
    /// The rule of <paramref name="key"/>, decided once per key and cached: attribute names are few and repeat in
    /// every record, so one lookup replaces a string comparison per rule (measured by PrivacyBenchmarks).
    /// </summary>
    private KeyRule Rule(string key)
    {
        if (_rules.TryGetValue(key, out var rule)) return rule;

        rule = _idAttributes.Contains(key) ? KeyRule.Id
             : IsRedacted(key) ? KeyRule.Redacted
             : _hostAttributes.Contains(key) ? KeyRule.Host
             : _urlAttributes.Contains(key) ? KeyRule.Url
             : key == BelemetrySemanticConventions.UrlQuery ? KeyRule.Query
             : IsVersionKey(key) ? KeyRule.Version
             : KeyRule.None;

        // A counter of its own: ConcurrentDictionary.Count takes every one of its locks.
        if (Volatile.Read(ref _cachedKeys) < MaxCachedKeys && _rules.TryAdd(key, rule)) Interlocked.Increment(ref _cachedKeys);
        return rule;
    }

    /// <summary>
    /// Whether <paramref name="key"/> is one of <see cref="BelemetryPrivacyOptions.RedactedAttributes"/>: the key whole
    /// or its last dot-separated segment equals a name, or the key starts with a prefix pattern.
    /// </summary>
    private bool IsRedacted(string key)
    {
        var dot = key.LastIndexOf('.');
        return _redactedNames.Contains(key)
               || (dot >= 0 && dot < key.Length - 1 && _redactedNames.Contains(key[(dot + 1)..]))
               || _redactedPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Whether a key names a version (<c>version</c> as its last segment, or ending in <c>_version</c> or a
    /// PascalCase <c>Version</c>): its four-part values are versions, not addresses. Matches the script.
    /// </summary>
    internal static bool IsVersionKey(string key)
    {
        var dot = key.LastIndexOf('.');
        return key.AsSpan(dot + 1).Equals("version", StringComparison.OrdinalIgnoreCase)
               || key.EndsWith("_version", StringComparison.OrdinalIgnoreCase)
               || key.EndsWith("Version", StringComparison.Ordinal);
    }

    private string[] ScrubAll(string[] texts, bool addresses)
    {
        string[]? scrubbed = null;
        for (var i = 0; i < texts.Length; i++)
        {
            var text = Scrub(texts[i], addresses);
            if (ReferenceEquals(text, texts[i])) continue;
            scrubbed ??= (string[])texts.Clone();
            scrubbed[i] = text;
        }

        return scrubbed ?? texts;
    }

    /// <summary>Redacts sensitive query parameters, strips the query or fragment as configured, then scrubs patterns.</summary>
    public string RedactUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        // Most URLs carry no query, no fragment and no credentials, and nothing a pattern could match anywhere in them:
        // there is nothing to rebuild (measured by PrivacyBenchmarks).
        if (url.AsSpan().IndexOfAny('?', '#', '@') < 0 && (options.ScrubPatterns is false || MayMatchPattern(url) is false)) return url;

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

            // A fragment route can carry a query of its own ("#/callback?access_token=..."): the name starts after it.
            var start = parts[i].LastIndexOf('?', eq - 1) + 1;
            var name = Uri.UnescapeDataString(parts[i][start..eq].Replace('+', ' '));
            if (_redactKeys.Contains(name)) parts[i] = parts[i][..(eq + 1)] + Redacted;
        }

        return string.Join('&', parts);
    }

    /// <summary>
    /// Replaces bearer tokens, JWTs, e-mail addresses, Luhn-valid card numbers and IPv4 addresses, in that order (the
    /// script's order, so both sides give the same text: <c>Bearer eyJ...</c> is a bearer token, not a JWT). Returns
    /// the same instance when nothing matched.
    /// </summary>
    public string Scrub(string text) => Scrub(text, addresses: true);

    /// <param name="text">The text.</param>
    /// <param name="addresses">Whether IPv4 addresses are scrubbed too (not in a value named like a version).</param>
    private string Scrub(string text, bool addresses)
    {
        if (options.ScrubPatterns is false || string.IsNullOrEmpty(text)) return text;

        // Every string attribute goes through here, and most (methods, routes, names, enum-like values) match no
        // pattern at all: a character scan per pattern is far cheaper than running its regex.
        if (text.Contains("bearer", StringComparison.OrdinalIgnoreCase)) text = BearerRegex().Replace(text, "Bearer [token]");
        if (text.Contains("eyJ", StringComparison.Ordinal)) text = JwtRegex().Replace(text, "[jwt]");
        if (text.Contains('@', StringComparison.Ordinal)) text = EmailRegex().Replace(text, "[email]");

        // A card number needs 13 digits and an IPv4 address 3 dots: a URL with an id in it, a version, a status text
        // has neither, and one pass over it is cheaper than running the two regexes (measured by PrivacyBenchmarks).
        var (digits, dots) = CountDigitsAndDots(text);
        if (digits >= 13) text = CardRegex().Replace(text, m => IsLuhnValid(m.Value) ? "[card]" : m.Value);
        if (addresses && digits >= 4 && dots >= 3) text = Ipv4Regex().Replace(text, "[ip]");

        return text;
    }

    /// <summary>Whether any pattern's pre-check passes on <paramref name="text"/> - false means <see cref="Scrub(string)"/> changes nothing.</summary>
    private static bool MayMatchPattern(string text)
    {
        if (text.Contains("bearer", StringComparison.OrdinalIgnoreCase) || text.Contains("eyJ", StringComparison.Ordinal) || text.Contains('@', StringComparison.Ordinal)) return true;
        var (digits, dots) = CountDigitsAndDots(text);
        return digits >= 13 || (digits >= 4 && dots >= 3);
    }

    private static (int Digits, int Dots) CountDigitsAndDots(string text)
    {
        int digits = 0, dots = 0;
        foreach (var c in text)
        {
            if (c is >= '0' and <= '9') digits++;
            else if (c == '.') dots++;
        }
        return (digits, dots);
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

    // The signature may be empty: an unsigned token (alg "none") still carries its claims.
    [GeneratedRegex(@"\beyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]*", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9._~+/\-]+=*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"\b(?:\d[ \-]?){12,18}\d\b", RegexOptions.CultureInvariant)]
    private static partial Regex CardRegex();

    // The lookbehind keeps assembly versions in .NET stack traces ("Version=10.0.0.0") from reading as addresses.
    [GeneratedRegex(@"(?<!Version=)\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b", RegexOptions.CultureInvariant)]
    private static partial Regex Ipv4Regex();
}
