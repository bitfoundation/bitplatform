//+:cnd:noEmit
using System.Net;
using System.Net.Sockets;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Services;

/// <summary>
/// Turns a <c>client_id</c> into something we know enough about to show a user and to redirect to. Since MCP
/// 2026-07-28 that id may be an https url pointing at a metadata document the client hosts itself, so fetching it and
/// checking that it names itself <i>is</i> the registration; the operator's configuration is the only fallback.
/// </summary>
public partial class OAuthClientResolver(IFusionCache cache,
    IHttpClientFactory httpClientFactory,
    ServerApiSettings appSettings,
    ILogger<OAuthClientResolver> logger)
{
    public const string HttpClientName = "OAuthClientMetadata";

    /// <summary>The longest client id this server stores, on a session and on a code. A metadata document url that
    /// does not fit is refused rather than truncated into one that matches nothing.</summary>
    public const int MaxClientIdLength = 512;

    // Cached so consent does not wait on someone else's uptime, short enough that changed redirect uris are not stale
    // for a day.
    private static readonly TimeSpan metadataCacheDuration = TimeSpan.FromHours(6);

    // Failures are cached too, so a client that is down does not make every authorization wait out its timeout - but
    // briefly, so a fixed one is not stuck behind the failure.
    private static readonly TimeSpan failureCacheDuration = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Null when the client cannot be identified. Callers must render that, never redirect on it: with no trustworthy
    /// redirect uri, honouring the one in the request is an open redirect.
    /// </summary>
    public async Task<OAuthClient?> Resolve(string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > MaxClientIdLength)
            return null;

        if (IsMetadataDocumentUrl(clientId, out var metadataUri) is false)
            return ResolveConfigured(clientId);

        return await cache.GetOrSetAsync<OAuthClient?>($"oauth-client:{clientId}",
            async (context, token) =>
            {
                var client = await FetchMetadataDocument(clientId, metadataUri!, token);

                if (client is null)
                {
                    context.Options.Duration = failureCacheDuration;
                }

                return client;
            },
            options => options.SetDuration(metadataCacheDuration),
            token: cancellationToken);
    }

    // https and a path are both required by the draft; the path stops a bare origin standing in for every client on it.
    private static bool IsMetadataDocumentUrl(string clientId, out Uri? metadataUri)
    {
        metadataUri = null;

        if (Uri.TryCreate(clientId, UriKind.Absolute, out var uri) is false)
            return false;

        if (uri.Scheme is not "https" || uri.AbsolutePath is "/" || string.IsNullOrEmpty(uri.Fragment) is false)
            return false;

        if (IsInternalHost(uri))
            return false;

        metadataUri = uri;
        return true;
    }

    /// <summary>
    /// A client id is a url this server fetches, chosen by whoever started the request, so it is an SSRF reach into
    /// whatever this host can see. Redirects are off, and a literal address in one of these ranges is never a client.
    /// Names that resolve to a private address still pass - a check on one lookup does not bind the request's own
    /// (DNS rebinding); network egress rules close that.
    /// </summary>
    private static bool IsInternalHost(Uri uri)
    {
        if (uri.IsLoopback)
            return true;

        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) is false)
            return false;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
            return true;

        if (address.AddressFamily is not AddressFamily.InterNetwork)
            return false;

        var octets = address.GetAddressBytes();

        return octets[0] is 10 or 127 or 0                                  // private, loopback, "this host"
               || (octets[0] is 172 && octets[1] >= 16 && octets[1] <= 31)  // private
               || (octets[0] is 192 && octets[1] is 168)                    // private
               || (octets[0] is 169 && octets[1] is 254)                    // link local, and 169.254.169.254 with it
               || (octets[0] is 100 && octets[1] >= 64 && octets[1] <= 127); // carrier grade nat
    }

    /// <summary>The only kind consent presents as vouched for, because a human here put it in configuration.</summary>
    private OAuthClient? ResolveConfigured(string clientId)
    {
        var client = appSettings.OAuth.Clients.FirstOrDefault(c => string.Equals(c.ClientId, clientId, StringComparison.Ordinal));

        if (client is null)
            return null;

        return new OAuthClient
        {
            ClientId = client.ClientId,
            ClientName = client.ClientName ?? client.ClientId,
            RedirectUris = client.RedirectUris ?? [],
            IsPreRegistered = true
        };
    }

    /// <summary>
    /// Null for a document that cannot be fetched, is not the client's own, or registers nothing acceptable: that is a
    /// client we cannot identify, not an error of ours - a timeout included, or a slow host becomes our 500.
    /// </summary>
    private async Task<OAuthClient?> FetchMetadataDocument(string clientId, Uri metadataUri, CancellationToken cancellationToken)
    {
        ClientIdMetadataDocument? document;

        try
        {
            using var httpClient = httpClientFactory.CreateClient(HttpClientName);

            // Buffered on purpose: Timeout and MaxResponseContentBufferSize only bound a response read whole, and
            // GetFromJsonAsync streams instead - a host that trickles could hold this request open indefinitely.
            using var response = await httpClient.GetAsync(metadataUri, HttpCompletionOption.ResponseContentRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            document = await response.Content.ReadFromJsonAsync(ServerJsonContext.Default.ClientIdMetadataDocument, cancellationToken);
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested is false)
        {
            logger.LogWarning(exception, "Could not fetch the client id metadata document at {MetadataUri}.", metadataUri);
            return null;
        }

        // The security property of the whole scheme: otherwise a document naming somebody else's client_id borrows
        // their identity on our consent screen.
        if (document is null || string.Equals(document.ClientId, clientId, StringComparison.Ordinal) is false)
        {
            logger.LogWarning("The metadata document at {MetadataUri} does not identify itself as that client.", metadataUri);
            return null;
        }

        if (document.RedirectUris is null or { Length: 0 })
        {
            logger.LogWarning("The metadata document at {MetadataUri} registers no redirect uris.", metadataUri);
            return null;
        }

        // The same rule dynamic registration applies, or a document could name a remote http uri and take the code
        // back to it in the clear.
        if (document.RedirectUris.All(OAuthService.IsAcceptableRedirectUri) is false)
        {
            logger.LogWarning("The metadata document at {MetadataUri} registers a redirect uri that is not acceptable.", metadataUri);
            return null;
        }

        return new OAuthClient
        {
            ClientId = clientId,
            ClientName = string.IsNullOrWhiteSpace(document.ClientName) ? OAuthClient.OriginOf(clientId) : document.ClientName,
            RedirectUris = document.RedirectUris,
            IsPreRegistered = false
        };
    }
}

/// <summary>A resolved client, however it was resolved.</summary>
public partial class OAuthClient
{
    public string ClientId { get; set; } = default!;

    /// <summary>Untrusted text supplied by the client. Never render as markup.</summary>
    public string ClientName { get; set; } = default!;

    public string[] RedirectUris { get; set; } = [];

    public bool IsPreRegistered { get; set; }

    /// <summary>
    /// The host serving the client's metadata document - the one part it cannot lie about - or the id itself when it
    /// is not a url. Consent and the management page both show this, from here, so they cannot disagree.
    /// </summary>
    public static string OriginOf(string clientId)
    {
        return Uri.TryCreate(clientId, UriKind.Absolute, out var uri) ? uri.Host : clientId;
    }

    /// <summary>
    /// Exact string comparison: any normalisation is somewhere to hide a second destination that compares equal. The
    /// loopback port is the one exception, and RFC 8252 §7.3 requires it - Visual Studio registers :33419 and arrives
    /// on another.
    /// </summary>
    public bool IsRegisteredRedirectUri(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return false;

        if (RedirectUris.Contains(redirectUri, StringComparer.Ordinal))
            return true;

        return Uri.TryCreate(redirectUri, UriKind.Absolute, out var requested)
               && IsLoopback(requested)
               && RedirectUris.Any(registered => Uri.TryCreate(registered, UriKind.Absolute, out var known)
                                                 && IsLoopback(known)
                                                 && MatchesIgnoringPort(known, requested));
    }

    // Both sides must be loopback, so a remote registration can never be satisfied by a loopback uri or vice versa.
    private static bool MatchesIgnoringPort(Uri registered, Uri requested)
    {
        return string.Equals(registered.Scheme, requested.Scheme, StringComparison.Ordinal)
               && string.Equals(registered.Host, requested.Host, StringComparison.OrdinalIgnoreCase)
               && string.Equals(registered.AbsolutePath, requested.AbsolutePath, StringComparison.Ordinal)
               && string.Equals(registered.Query, requested.Query, StringComparison.Ordinal)
               && string.IsNullOrEmpty(requested.Fragment);
    }

    // The rfc prefers the ip literal, but clients do use localhost and refusing it only refuses them.
    private static bool IsLoopback(Uri uri)
    {
        return uri.Host is "127.0.0.1" or "::1" or "[::1]"
               || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>The subset of draft-ietf-oauth-client-id-metadata-document-00 this server reads.</summary>
public partial class ClientIdMetadataDocument
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; set; }
}
