namespace Bit.Belemetry;

/// <summary>
/// A Sentry DSN (<c>{scheme}://{public key}[:{secret}]@{host}[:{port}][/{path}]/{project id}</c>) and the envelope
/// endpoint it names. Mirrored by <c>parseDsn</c> in <c>Scripts/exporters/sentry.ts</c>.
/// </summary>
internal sealed class SentryDsn
{
    private SentryDsn(string publicKey, string projectId, string envelopeEndpoint, string publicDsn)
    {
        PublicKey = publicKey;
        ProjectId = projectId;
        EnvelopeEndpoint = envelopeEndpoint;
        EnvelopeUrl = envelopeEndpoint + "?sentry_key=" + Uri.EscapeDataString(publicKey) + "&sentry_version=7";
        PublicDsn = publicDsn;
    }

    /// <summary>The project's public key: what authenticates an event.</summary>
    public string PublicKey { get; }

    /// <summary>The project id, the DSN's last path segment.</summary>
    public string ProjectId { get; }

    /// <summary><c>{scheme}://{host}{path}/api/{project}/envelope/</c>, without the authentication.</summary>
    public string EnvelopeEndpoint { get; }

    /// <summary>
    /// The URL events are posted to: <see cref="EnvelopeEndpoint"/> with the key in the query, as Sentry's browser SDK
    /// sends it - it needs no header, so it works from a page and for a stored request alike.
    /// </summary>
    public string EnvelopeUrl { get; }

    /// <summary>The DSN without a legacy secret key: what the envelopes and the browser script carry.</summary>
    public string PublicDsn { get; }

    /// <summary>Parses <paramref name="dsn"/>; throws <see cref="FormatException"/> when it is not one.</summary>
    public static SentryDsn Parse(string dsn)
    {
        if (Uri.TryCreate(dsn?.Trim(), UriKind.Absolute, out var uri) is false || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new FormatException("A Sentry DSN is an absolute http(s) URL.");
        }

        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        var colon = userInfo.IndexOf(':', StringComparison.Ordinal);
        var publicKey = colon >= 0 ? userInfo[..colon] : userInfo;
        if (publicKey.Length == 0) throw new FormatException("A Sentry DSN carries the project's public key as its user name.");

        var path = uri.AbsolutePath.TrimEnd('/');
        var slash = path.LastIndexOf('/');
        var projectId = Uri.UnescapeDataString(path[(slash + 1)..]);
        if (projectId.Length == 0) throw new FormatException("A Sentry DSN ends with the project id.");

        var server = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
        var prefix = path[..Math.Max(0, slash)];
        var escapedProject = Uri.EscapeDataString(projectId);
        return new SentryDsn(publicKey, projectId, $"{server}{prefix}/api/{escapedProject}/envelope/",
                             $"{uri.Scheme}://{Uri.EscapeDataString(publicKey)}@{uri.Authority}{prefix}/{escapedProject}");
    }
}
