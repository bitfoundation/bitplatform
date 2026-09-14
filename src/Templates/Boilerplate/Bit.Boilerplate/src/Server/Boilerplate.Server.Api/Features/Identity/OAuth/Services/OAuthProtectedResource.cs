//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Services;

/// <summary>
/// RFC 9728 protected resource metadata, and the <c>WWW-Authenticate</c> header pointing at it. By hand rather than
/// through <c>AddMcp()</c>, whose metadata is fixed absolute uris at startup - that would undo the request-derived
/// issuer that lets one build serve localhost, a dev tunnel and production.
/// </summary>
public static class OAuthProtectedResource
{
    /// <summary>RFC 9728 §3.1 puts the well-known segment between the host and the resource's own path.</summary>
    public const string WellKnownPath = "/.well-known/oauth-protected-resource";

    public static string MetadataUrl(string issuer, string resourcePath)
    {
        return $"{issuer}{WellKnownPath}{resourcePath}";
    }

    /// <summary>The 401 challenge; naming the scope means the client asks for what this resource needs, not everything.</summary>
    public static string ChallengeHeaderValue(string issuer, string resourcePath)
    {
        var scopes = string.Join(' ', OAuthResources.ScopesFor(resourcePath));

        return $"Bearer resource_metadata=\"{MetadataUrl(issuer, resourcePath)}\", scope=\"{scopes}\"";
    }

    /// <summary>
    /// The document itself. The issuer is passed in so every caller derives it the one way, <c>GetIssuer()</c> - a
    /// client that discovers one issuer here and another there aborts on the mismatch (RFC 9207).
    /// </summary>
    public static object Metadata(string issuer, string resourcePath)
    {
        return new
        {
            resource = $"{issuer}{resourcePath}",
            // One authorization server: this app. The client discovers the rest from that server's own metadata.
            authorization_servers = new[] { issuer },
            scopes_supported = OAuthResources.ScopesFor(resourcePath),
            bearer_methods_supported = new[] { "header" },
            resource_documentation = $"{issuer}/scalar"
        };
    }
}
