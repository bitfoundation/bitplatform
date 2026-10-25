using System.Reflection;
using System.Text.RegularExpressions;

namespace Bit.BevTools;

/// <summary>
/// Decides what is sensitive: member, header, query and key names (case-insensitive substring match plus
/// <see cref="BevToolsSensitiveAttribute"/>), and values whose shape is a credential whatever they are called.
/// </summary>
public sealed partial class BevRedactor
{
    private static readonly Regex JwtPattern = new(@"ey[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]{6,}\.", RegexOptions.CultureInvariant);

    public const string RedactedValue = "«redacted»";

    /// <summary>Shown instead of a value BevTools did not read at all, because <see cref="BitBevToolsOptions.ValueAccess"/> forbids it.</summary>
    public const string HiddenValue = "«hidden»";

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

    /// <summary>
    /// True when a value is a credential by its shape, whatever the member, key or header holding it is called: a JSON
    /// Web Token, a bearer token, a PEM private key, a connection string or key/value text that carries a password or
    /// key, and the token formats of the common cloud and source-control providers. Names catch the secrets that are
    /// labeled; this catches the ones held in a <c>string Value</c>, a claim, a dictionary of headers or a log message.
    /// Kept in step with <c>looksLikeSecret</c> in bit-bevtools.ts, which applies the same test to browser storage.
    /// </summary>
    public static bool LooksLikeSecret(string? value) =>
        value is { Length: >= 8 } && (JwtPattern.IsMatch(value) || SecretShapes().IsMatch(value));

    /// <summary>
    /// Replaces the credentials inside free text - an exception or log message, a JavaScript error - with
    /// <see cref="RedactedValue"/> and keeps the rest readable: "Login failed for token «redacted»" says more than a
    /// message hidden whole. Text without anything credential-shaped is returned as it is, without allocating.
    /// </summary>
    public static string RedactText(string text)
    {
        if (string.IsNullOrEmpty(text) || !LooksLikeSecret(text))
        {
            return text;
        }

        text = JwtTokens().Replace(text, RedactedValue);
        text = BearerTokens().Replace(text, "${scheme} " + RedactedValue);
        text = PrivateKeyBlocks().Replace(text, RedactedValue);
        text = KeyValueSecrets().Replace(text, "${key}${sep}" + RedactedValue);
        return ProviderTokens().Replace(text, RedactedValue);
    }

    // The key/value form: "Password=x;" in a connection string, "api_key: x" in config text, "access_token":"x" in JSON.
    private const string SecretKeyNames = @"password|passwd|pwd|secret|client[_-]?secret|api[_-]?key|access[_-]?key|account[_-]?key|shared[_-]?access[_-]?(?:key|signature)|access[_-]?token|id[_-]?token|refresh[_-]?token|authorization|credential|private[_-]?key";

    private const string ProviderTokenFormats =
        @"\bAKIA[0-9A-Z]{16}\b"                       // AWS access key id
        + @"|\bgh[pousr]_[A-Za-z0-9]{30,}\b"          // GitHub tokens
        + @"|\bgithub_pat_[A-Za-z0-9_]{40,}\b"
        + @"|\bxox[abposr]-[A-Za-z0-9-]{10,}\b"       // Slack
        + @"|\b[rs]k_(?:live|test)_[A-Za-z0-9]{16,}\b" // Stripe
        + @"|\bAIza[0-9A-Za-z_-]{35}\b"               // Google API keys
        + @"|\bsk-[A-Za-z0-9_-]{32,}\b";              // OpenAI-style secret keys

    // A value is anything up to the next separator, except what redaction already put there: text that went through
    // RedactText once must not look like a secret again ("Password=«redacted»").
    private const string SecretValue = @"(?!" + RedactedValue + @")(?!Bearer\s)[^\s;,""'&]{3,}";

    [GeneratedRegex(@"\bBearer\s+(?!" + RedactedValue + @")[A-Za-z0-9._~+/=-]+|-----BEGIN [A-Z ]*PRIVATE KEY-----|(?:^|[\s;,{""'&?])(?:" + SecretKeyNames + @")[""']?\s*[:=]\s*[""']?(?:Bearer\s+)?" + SecretValue + "|" + ProviderTokenFormats, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SecretShapes();

    [GeneratedRegex(@"ey[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]*", RegexOptions.CultureInvariant)]
    private static partial Regex JwtTokens();

    [GeneratedRegex(@"(?<scheme>\bBearer)\s+[A-Za-z0-9._~+/=-]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokens();

    [GeneratedRegex(@"-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?(?:-----END [A-Z ]*PRIVATE KEY-----|$)", RegexOptions.CultureInvariant)]
    private static partial Regex PrivateKeyBlocks();

    // "Authorization: Bearer x" keeps its scheme: the bearer pass already replaced the token after it.
    [GeneratedRegex(@"(?<key>(?<![A-Za-z0-9])(?:" + SecretKeyNames + @")[""']?)(?<sep>\s*[:=]\s*[""']?(?:Bearer\s+)?)" + SecretValue, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex KeyValueSecrets();

    [GeneratedRegex(ProviderTokenFormats, RegexOptions.CultureInvariant)]
    private static partial Regex ProviderTokens();

    /// <summary>
    /// Redacts embedded credentials, and query-string and fragment values whose key looks sensitive; keeps everything
    /// else intact. Works on absolute and on base-relative URLs (navigation targets).
    /// </summary>
    public string RedactUrl(string url)
    {
        url = RedactUserInfo(url);
        var hash = url.IndexOf('#');
        var beforeFragment = hash < 0 ? url : url[..hash];
        var q = beforeFragment.IndexOf('?');
        var redacted = q < 0 ? beforeFragment : beforeFragment[..(q + 1)] + RedactParameters(beforeFragment[(q + 1)..]);
        if (hash < 0)
        {
            return redacted;
        }

        // OAuth implicit and hybrid flows return their tokens in the fragment (#access_token=…&id_token=…), which a
        // client-side router sees on every navigation.
        var fragment = url[(hash + 1)..];
        return redacted + "#" + (fragment.Contains('=') ? RedactParameters(fragment) : fragment);
    }

    private string RedactParameters(string parameters)
    {
        var parts = parameters.Split('&');
        var changed = false;
        for (var i = 0; i < parts.Length; i++)
        {
            var eq = parts[i].IndexOf('=');
            var key = eq < 0 ? parts[i] : parts[i][..eq];
            var value = eq < 0 ? string.Empty : Uri.UnescapeDataString(parts[i][(eq + 1)..].Replace('+', ' '));
            // "sig" is the signature of an Azure shared access signature URL: the part of it that grants access. Too
            // short a word to be a name pattern, so it is matched exactly, and only here.
            var name = Uri.UnescapeDataString(key);
            if (IsSensitiveName(name) || string.Equals(name, "sig", StringComparison.OrdinalIgnoreCase) || LooksLikeSecret(value))
            {
                parts[i] = key + "=" + RedactedValue;
                changed = true;
            }
        }

        return changed ? string.Join('&', parts) : parameters;
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
