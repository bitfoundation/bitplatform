//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Services;

/// <summary>
/// The RFC 8707 resources this server issues tokens for. A resource becomes the <c>aud</c> of its token, so this list
/// is exactly the set of places an OAuth token can ever be used.
/// </summary>
public static class OAuthResources
{
    public const string DevMcpPath = "/dev-mcp";

    //#if (signalR == true)
    /// <summary>
    /// The chatbot's tools. Only the server-side ones are exposed there; the rest drive the user's live browser over
    /// SignalR, which consent to "use the chatbot" does not cover, so they are not MCP tools at all.
    /// </summary>
    public const string McpPath = "/mcp";
    //#endif

    /// <summary>
    /// One row per resource: its path, the scopes worth asking for, and the policies its endpoint requires on top of a
    /// signed-in user. The endpoint mapping, the metadata, the 401 challenge and consent all read the same row, so a
    /// requirement cannot be added to an endpoint without consent refusing the users who fail it.
    /// </summary>
    public sealed record OAuthResource(string Path, string[] Scopes, string[] Policies);

    private static readonly OAuthResource[] resources = [
        //#if (signalR == true)
        // The chatbot asks nothing of the caller but a signed-in user, which every policy set implies.
        new(McpPath, [OAuthScopes.Chat], []),
        //#endif
        new(DevMcpPath, [OAuthScopes.DevMcp], [AppFeatures.System.DevMcp, AuthPolicies.TFA_ENABLED])
    ];

    /// <summary>
    /// The resource that serves <paramref name="resourceOrPath"/> - a canonical resource url, the resource's own path,
    /// or any request path beneath it - or null when nothing of the sort is served.
    /// </summary>
    public static OAuthResource? Find(string resourceOrPath)
    {
        var path = PathOf(resourceOrPath);

        return resources.FirstOrDefault(resource => Covers(resource.Path, path));
    }

    /// <summary>
    /// What a resource is worth asking for. RFC 6749 §3.3 lets a client omit <c>scope</c> - Visual Studio does - and
    /// without a default every such client is turned away with <c>invalid_scope</c>.
    /// </summary>
    public static string[] ScopesFor(string resourceOrPath) => Find(resourceOrPath)?.Scopes ?? [];

    /// <summary>
    /// The policies the resource's endpoint requires. Consent evaluates them against the granting session, because a
    /// grant can only carry what that session has - its <c>amr</c>, for one.
    /// </summary>
    public static string[] PoliciesFor(string resourceOrPath) => Find(resourceOrPath)?.Policies ?? [];

    /// <summary>
    /// Whether a token minted for <paramref name="canonicalResource"/> may be spent on <paramref name="requestPath"/>.
    /// Being <i>a</i> resource this deployment serves is not enough, or a chatbot token would authenticate at
    /// <c>/dev-mcp</c> - RFC 8707 exists so an audience names one resource rather than the server.
    /// </summary>
    public static bool CoversPath(string canonicalResource, string issuer, string requestPath)
    {
        if (canonicalResource.StartsWith(issuer, StringComparison.Ordinal) is false)
            return false;

        return Covers(canonicalResource[issuer.Length..], requestPath);
    }

    public static string[] All(string issuer) => [.. resources.Select(resource => $"{issuer}{resource.Path}")];

    /// <summary>
    /// Maps what the client sent onto the canonical spelling, or false if nothing of the sort is served. Scheme and
    /// host fold case and a trailing slash is ignored, as MCP asks; the path is exact, or <c>/DEV-MCP</c> would mint a
    /// token for <c>/dev-mcp</c>.
    /// </summary>
    public static bool TryNormalize(string issuer, string? resource, out string canonicalResource)
    {
        canonicalResource = default!;

        if (string.IsNullOrWhiteSpace(resource) || Uri.TryCreate(resource, UriKind.Absolute, out var resourceUri) is false)
            return false;

        if (string.IsNullOrEmpty(resourceUri.Fragment) is false)
            return false; // RFC 8707: a resource identifier must not carry a fragment.

        if (Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) is false)
            return false;

        if (Uri.Compare(resourceUri, issuerUri, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) is not 0)
            return false;

        var path = resourceUri.AbsolutePath.TrimEnd('/');

        var match = resources.FirstOrDefault(known => string.Equals(known.Path, path, StringComparison.Ordinal));

        if (match is null)
            return false;

        canonicalResource = $"{issuer}{match.Path}";
        return true;
    }

    // A prefix only counts at a segment boundary, or /mcp would cover /mcp-admin.
    private static bool Covers(string resourcePath, string requestPath)
    {
        return requestPath.StartsWith(resourcePath, StringComparison.Ordinal)
               && (requestPath.Length == resourcePath.Length || requestPath[resourcePath.Length] is '/');
    }

    private static string PathOf(string resourceOrPath)
    {
        if (resourceOrPath.StartsWith('/'))
            return resourceOrPath;

        return Uri.TryCreate(resourceOrPath, UriKind.Absolute, out var uri) ? uri.AbsolutePath : resourceOrPath;
    }
}
