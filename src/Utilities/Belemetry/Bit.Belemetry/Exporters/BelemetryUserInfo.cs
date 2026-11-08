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
    /// <summary>Whether <paramref name="uri"/> is an absolute URI with user info (<c>user:password@</c>); a relative one has none.</summary>
    public static bool Has(Uri? uri) => uri is { IsAbsoluteUri: true, UserInfo.Length: > 0 };

    /// <summary><paramref name="uri"/> without its user info; one with none is returned as it is.</summary>
    public static Uri? Remove(Uri? uri) => Has(uri) ? new UriBuilder(uri!) { UserName = "", Password = "" }.Uri : uri;

    /// <summary><paramref name="url"/> without its user info, for a message; one with none is returned as it is.</summary>
    public static string Remove(string url) =>
        url.Contains('@', StringComparison.Ordinal) && Uri.TryCreate(url, UriKind.Absolute, out var uri) && Has(uri) ? Remove(uri)!.ToString() : url;
}
