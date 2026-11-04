using System.Collections;
using System.Globalization;

namespace Bit.Belemetry;

/// <summary>
/// The one sanitization pipeline every diagnostic value goes through - event attributes, messages, exception text,
/// snapshot trees, release metadata, browser evidence - on the way in, before it is held in memory, and again when a
/// bundle is written. In order: key redaction (the privacy options' <see cref="BelemetryPrivacyOptions.RedactedAttributes"/>
/// and blocked keys, plus <see cref="BelemetryDiagnosticsOptions.RedactedKeys"/>), the application's
/// <see cref="IBelemetryDiagnosticSanitizer"/>s, pattern scrubbing (always on here, whatever
/// <see cref="BelemetryPrivacyOptions.ScrubPatterns"/> says for export), then the size limits.
/// </summary>
/// <remarks>A failure anywhere excludes the value: nothing is ever kept because sanitizing it failed.</remarks>
internal sealed class BelemetryDiagnosticSanitizer
{
    internal const string Redacted = "REDACTED";
    internal const string Unsupported = "[unsupported]";
    internal const string Truncated = "[truncated]";

    // Diagnostics are handed to people: a key that merely contains one of these ("couponSecret", "refreshTokenValue",
    // "dbConnectionString") is redacted too. Redacting too much is the safe way to be wrong here.
    private static readonly string[] _sensitiveFragments =
    [
        "password", "passwd", "secret", "token", "apikey", "api_key", "api-key", "authorization", "cookie", "credential",
        "privatekey", "private_key", "connectionstring", "connection_string", "signature", "creditcard", "cardnumber", "cvv",
    ];

    private readonly BelemetryPrivacy _privacy;
    private readonly IBelemetryDiagnosticSanitizer[] _custom;
    private readonly HashSet<string> _redactedNames;
    private readonly string[] _redactedPrefixes;
    private readonly HashSet<string> _allowedAttributes;

    public BelemetryDiagnosticSanitizer(BelemetryOptions options, IEnumerable<IBelemetryDiagnosticSanitizer>? custom = null)
    {
        // Pattern scrubbing is forced on: an operator may turn it off for what the backend receives, but a bundle is
        // handed to people, often outside the team that operates the backend.
        var privacy = new BelemetryPrivacyOptions { StripQueryString = options.Privacy.StripQueryString, StripFragment = true, ScrubPatterns = true };
        privacy.RedactQueryParameters.Clear();
        privacy.RedactQueryParameters.AddRange(options.Privacy.RedactQueryParameters);
        privacy.RedactedAttributes.Clear();
        privacy.RedactedAttributes.AddRange(options.Privacy.RedactedAttributes);
        privacy.BlockedAttributes.AddRange(options.Privacy.BlockedAttributes);
        _privacy = new BelemetryPrivacy(privacy);

        var names = options.Diagnostics.RedactedKeys;
        _redactedNames = new HashSet<string>(names.Where(n => n.EndsWith('*') is false), StringComparer.OrdinalIgnoreCase);
        _redactedPrefixes = [.. names.Where(n => n.EndsWith('*')).Select(n => n[..^1])];
        _allowedAttributes = new HashSet<string>(options.Diagnostics.AllowedAttributes, StringComparer.Ordinal);
        _custom = custom?.ToArray() ?? [];
        Options = options.Diagnostics;
    }

    public BelemetryDiagnosticsOptions Options { get; }

    /// <summary>Whether the app allowlisted <paramref name="key"/> (<see cref="BelemetryDiagnosticsOptions.AllowedAttributes"/>).</summary>
    public bool IsAllowedAttribute(string key) => _allowedAttributes.Contains(key);

    /// <summary>
    /// Whether the value of <paramref name="key"/> is never kept: blocked or redacted by name (the privacy options'
    /// rules and <see cref="BelemetryDiagnosticsOptions.RedactedKeys"/>), or named like a credential anywhere in it.
    /// </summary>
    public bool IsRedactedKey(string key)
    {
        if (_privacy.HidesValue(key)) return true;

        var dot = key.LastIndexOf('.');
        if (_redactedNames.Contains(key)
            || (dot >= 0 && dot < key.Length - 1 && _redactedNames.Contains(key[(dot + 1)..]))
            || _redactedPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        foreach (var fragment in _sensitiveFragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>A text value scrubbed and cut to <paramref name="maxLength"/> characters.</summary>
    public string? Text(string? value, int maxLength, ref BelemetrySanitizeCounters counters)
    {
        if (value is null) return null;

        try
        {
            var scrubbed = _privacy.Scrub(value);
            if (ReferenceEquals(scrubbed, value) is false && string.Equals(scrubbed, value, StringComparison.Ordinal) is false) counters.Redactions++;
            if (maxLength > 0 && scrubbed.Length > maxLength)
            {
                counters.Truncations++;
                scrubbed = BelemetryValueLimit.Truncate(scrubbed, maxLength);
            }
            return scrubbed;
        }
        catch
        {
            counters.Omissions++;
            return null;
        }
    }

    /// <summary>A URL as the timeline keeps it: templated, with the allowlisted query parameters only, scrubbed.</summary>
    public string? Url(string? url, ref BelemetrySanitizeCounters counters)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var templated = BelemetryUrlTemplate.TemplateUrl(_privacy.RedactUrl(url), Options.AllowedQueryParameters);
            if (string.Equals(templated, url, StringComparison.Ordinal) is false) counters.Redactions++;
            return Text(templated, 2048, ref counters);
        }
        catch
        {
            counters.Omissions++;
            return null;
        }
    }

    /// <summary>
    /// One scalar attribute value: redacted by key, offered to the app's sanitizers, scrubbed and cut. Arrays become
    /// a comma-separated string of their first items. Null drops it.
    /// </summary>
    public object? Scalar(string key, object? value, ref BelemetrySanitizeCounters counters, int maxLength = 512)
    {
        if (value is null) return null;
        if (IsRedactedKey(key))
        {
            counters.Redactions++;
            return Redacted;
        }

        if (Custom(key, value, ref counters, out var custom)) value = custom;
        if (value is null) return null;

        switch (value)
        {
            case string s:
                return Text(s, maxLength, ref counters);
            case bool or int or long or short or byte or sbyte or ushort or uint:
                return value;
            case ulong u:
                return u <= long.MaxValue ? (long)u : u.ToString(CultureInfo.InvariantCulture);
            case double d:
                return double.IsFinite(d) ? d : null;
            case float f:
                return float.IsFinite(f) ? (double)f : null;
            case decimal m:
                return (double)m;
            case IEnumerable items:
                var parts = new List<string>();
                foreach (var item in items)
                {
                    if (parts.Count == 10)
                    {
                        counters.Truncations++;
                        break;
                    }
                    if (item is not null) parts.Add(BelemetryAttributes.ToInvariantString(item));
                }
                return Text(string.Join(',', parts), maxLength, ref counters);
            default:
                counters.Omissions++;
                return null;
        }
    }

    /// <summary>
    /// A snapshot's data as a tree of supported values, within <paramref name="limits"/>: never an object walked by
    /// reflection - an unsupported value becomes <see cref="Unsupported"/>.
    /// </summary>
    public Dictionary<string, object?> Tree(IReadOnlyDictionary<string, object?> data, BelemetrySnapshotOptions limits, ref BelemetrySanitizeCounters counters)
    {
        var budget = Math.Max(256, limits.MaxBytes);
        return (Dictionary<string, object?>)Node(string.Empty, data, depth: 0, limits, ref budget, ref counters)!;
    }

    private object? Node(string path, object? value, int depth, BelemetrySnapshotOptions limits, ref int budget, ref BelemetrySanitizeCounters counters)
    {
        if (value is null) return null;

        if (path.Length > 0 && IsRedactedKey(LastSegment(path)))
        {
            counters.Redactions++;
            return Redacted;
        }

        if (path.Length > 0 && Custom(path, value, ref counters, out var custom))
        {
            value = custom;
            if (value is null) return null;
        }

        if (budget <= 0)
        {
            counters.Truncations++;
            return Truncated;
        }

        switch (value)
        {
            case string s:
                var text = Text(s, Math.Max(1, limits.MaxStringLength), ref counters);
                budget -= 8 + ((text?.Length ?? 0) * 2);
                return text;
            case bool or int or long or short or byte or sbyte or ushort or uint:
                budget -= 16;
                return value;
            case ulong u:
                budget -= 16;
                return u <= long.MaxValue ? (long)u : u.ToString(CultureInfo.InvariantCulture);
            case double d:
                budget -= 16;
                return double.IsFinite(d) ? d : null;
            case float f:
                budget -= 16;
                return float.IsFinite(f) ? (double)f : null;
            case decimal m:
                budget -= 16;
                return (double)m;
            case DateTime dt:
                budget -= 64;
                return dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            case DateTimeOffset dto:
                budget -= 64;
                return dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            case TimeSpan ts:
                budget -= 48;
                return ts.ToString("c", CultureInfo.InvariantCulture);
            case Guid guid:
                budget -= 80;
                return guid.ToString("D");
            case Enum e:
                budget -= 32;
                return e.ToString();
        }

        if (depth >= Math.Max(1, limits.MaxDepth))
        {
            counters.Truncations++;
            return Truncated;
        }

        var maxItems = Math.Max(1, limits.MaxItems);
        switch (value)
        {
            case IReadOnlyDictionary<string, object?> readOnly:
                return Map(path, readOnly, depth, limits, maxItems, ref budget, ref counters);
            case IDictionary dictionary:
                var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (copy.Count > maxItems) break;
                    copy[BelemetryAttributes.ToInvariantString(entry.Key)] = entry.Value;
                }
                return Map(path, copy, depth, limits, maxItems, ref budget, ref counters);
            case IEnumerable items:
                var list = new List<object?>();
                var index = 0;
                foreach (var item in items)
                {
                    if (list.Count == maxItems)
                    {
                        counters.Truncations++;
                        list.Add(Truncated);
                        break;
                    }
                    list.Add(Node(Join(path, index.ToString(CultureInfo.InvariantCulture)), item, depth + 1, limits, ref budget, ref counters));
                    index++;
                }
                return list;
            default:
                // An object: Belemetry never walks one (reflection, trimming, and no idea what it holds).
                counters.Omissions++;
                budget -= 32;
                return Unsupported;
        }
    }

    private Dictionary<string, object?> Map(string path, IReadOnlyDictionary<string, object?> map, int depth, BelemetrySnapshotOptions limits, int maxItems, ref int budget, ref BelemetrySanitizeCounters counters)
    {
        var result = new Dictionary<string, object?>(Math.Min(map.Count, maxItems + 1), StringComparer.Ordinal);
        foreach (var (key, item) in map.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (result.Count == maxItems)
            {
                counters.Truncations++;
                result["…"] = Truncated;
                break;
            }

            var safeKey = key.Length > 128 ? key[..128] : key;
            budget -= 16 + (safeKey.Length * 2);
            result[safeKey] = Node(Join(path, safeKey), item, depth + 1, limits, ref budget, ref counters);
        }
        return result;
    }

    private bool Custom(string path, object? value, ref BelemetrySanitizeCounters counters, out object? sanitized)
    {
        sanitized = value;
        foreach (var sanitizer in _custom)
        {
            try
            {
                if (sanitizer.TrySanitize(path, sanitized, out var next))
                {
                    if (ReferenceEquals(next, sanitized) is false && Equals(next, sanitized) is false) counters.Redactions++;
                    sanitized = next;
                    if (sanitized is null) return true;
                }
            }
            catch
            {
                // Never kept as it was because the app's sanitizer failed on it.
                counters.Omissions++;
                sanitized = null;
                return true;
            }
        }
        return ReferenceEquals(sanitized, value) is false;
    }

    private static string Join(string path, string key) => path.Length == 0 ? key : path + "." + key;

    private static string LastSegment(string path)
    {
        var dot = path.LastIndexOf('.');
        return dot < 0 ? path : path[(dot + 1)..];
    }
}

/// <summary>What sanitizing a value did: how many values were redacted, cut and left out.</summary>
internal struct BelemetrySanitizeCounters
{
    public int Redactions;
    public int Truncations;
    public int Omissions;

    public readonly bool Any => Redactions + Truncations + Omissions > 0;
}
