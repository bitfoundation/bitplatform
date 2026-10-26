namespace Bit.Balette;

/// <summary>Decides whether a command's <see cref="BaletteCommand.Href"/> may be followed.</summary>
internal static class BaletteUrl
{
    /// <summary>
    /// The scheme of <paramref name="href"/> when it is not in <paramref name="allowed"/>, or null when the link may be
    /// followed - relative links always may. The scheme is read the way a browser reads it: the URL standard strips
    /// leading and trailing C0 controls and spaces, and removes tabs and newlines anywhere, so <c>" java\tscript:..."</c>
    /// is a <c>javascript:</c> URL to the browser and must be one here too.
    /// </summary>
    public static string? UnsafeScheme(string href, ISet<string> allowed)
    {
        Span<char> buffer = href.Length <= 256 ? stackalloc char[href.Length] : new char[href.Length];
        var length = 0;
        foreach (var c in href.AsSpan().Trim(" \u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f"))
        {
            if (c is not ('\t' or '\n' or '\r'))
            {
                buffer[length++] = c;
            }
        }

        var url = buffer[..length];

        // scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." ) ":" - anything else before a ':' makes it a relative path.
        if (url.Length == 0 || !char.IsAsciiLetter(url[0]))
        {
            return null;
        }

        for (var i = 1; i < url.Length; i++)
        {
            var c = url[i];
            if (c == ':')
            {
                var scheme = url[..i].ToString();
                return allowed.Contains(scheme) ? null : scheme.ToLowerInvariant();
            }

            if (!(char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.'))
            {
                return null;
            }
        }

        return null;
    }
}
