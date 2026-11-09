using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// The user info of an exporter endpoint (<c>https://user:token@collector.example.com</c>, set in code or through
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>). It is a credential that authenticates nothing: .NET's HttpClient leaves it out of
/// its requests, and fetch refuses a URL that includes credentials. Belemetry therefore never repeats it - neither in
/// the browser script's configuration (<see cref="BelemetryBrowserConfigFactory"/>), which a server renders into every
/// page it serves, nor in its own warnings and diagnostics, which go to every log sink.
/// </summary>
internal static class BelemetryUserInfo
{
    private static readonly char[] AuthorityTerminators = ['/', '?', '#'];

    /// <summary>Whether <paramref name="uri"/> is an absolute URI with user info (<c>user:password@</c>); a relative one has none.</summary>
    public static bool Has(Uri? uri) => uri is { IsAbsoluteUri: true, UserInfo.Length: > 0 };

    /// <summary><paramref name="uri"/> without its user info; one with none is returned as it is.</summary>
    public static Uri? Remove(Uri? uri) => Has(uri) ? new UriBuilder(uri!) { UserName = "", Password = "" }.Uri : uri;

    /// <summary>
    /// <paramref name="url"/> without its user info, for a message; one with none is returned as it is. A URL no
    /// <see cref="Uri"/> parses loses it as text (<see cref="RemoveFromText"/>): a malformed one may hold a credential too.
    /// </summary>
    public static string Remove(string url)
    {
        if (url.Contains('@', StringComparison.Ordinal) is false) return url;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) is false) return RemoveFromText(url);
        return Has(uri) ? Remove(uri)!.ToString() : url;
    }

    /// <summary>
    /// <paramref name="text"/> without the user info of any authority in it (after <c>://</c>, or a leading <c>//</c>),
    /// every other character kept as it is: for a URL no <see cref="Uri"/> parses, and for a regular expression of URLs
    /// (<see cref="BelemetryBrowserOptions.IgnoreUrls"/>), which parsing would rewrite. As the privacy rules' URL
    /// scrubbing does, an authority ends at the first <c>/</c>, <c>?</c> or <c>#</c> and loses everything up to its last <c>@</c>.
    /// </summary>
    public static string RemoveFromText(string text)
    {
        if (text.Contains('@', StringComparison.Ordinal) is false) return text;

        StringBuilder? result = null;
        var copied = 0;
        var start = text.StartsWith("//", StringComparison.Ordinal) ? 2 : AuthorityAfter(text, 0);
        while (start >= 0)
        {
            var end = text.IndexOfAny(AuthorityTerminators, start);
            if (end < 0) end = text.Length;

            var at = text.AsSpan(start, end - start).LastIndexOf('@');
            if (at >= 0)
            {
                (result ??= new(text.Length)).Append(text, copied, start - copied);
                copied = start + at + 1;
            }

            start = AuthorityAfter(text, start);
        }

        return result is null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }

    private static int AuthorityAfter(string text, int from) => text.IndexOf("://", from, StringComparison.Ordinal) is var i and >= 0 ? i + 3 : -1;
}
