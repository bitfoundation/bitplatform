//+:cnd:noEmit

using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    internal static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        var att = context.GetEndpoint()?.Metadata.OfType<AppResponseCacheAttribute>().FirstOrDefault();

        if (att?.MaxAge == -1 && att?.SharedMaxAge == -1)
            throw new InvalidOperationException("Invalid configuration: Both MaxAge and SharedMaxAge are unset. At least one of them must be specified in the ResponseCache attribute.");

        return att;
    }

    //#if (api == "Integrated")
    internal static bool IsBlazorPageContext(this HttpContext context)
    {
        return context.GetEndpoint()?.Metadata?.OfType<ComponentTypeMetadata>()?.Any() is true;
    }
    //#endif

    /// <summary>
    /// Validates the authentication status of an incoming HTTP request in an API endpoint that supports anonymous access.
    /// If the request is unauthenticated (i.e., no valid user is associated with the context) but an Authorization header
    /// is present, it is assumed that the provided access token is likely expired. In this case, the method throws an
    /// <see cref="UnauthorizedException"/> to signal the client to refresh the access token and retry the request. This behavior ensures
    /// that clients with potentially expired tokens are prompted to re-authenticate while still allowing anonymous api call.
    /// This way, API can act differently based on the authentication status of the request, while still allowing anonymous access to certain endpoints.
    /// </summary>
    public static void ThrowIfContainsExpiredAccessToken(this HttpContext context)
    {
        if (context.ContainsExpiredAccessToken())
            throw new UnauthorizedException();
    }

    /// <summary>
    /// <inheritdoc cref="ThrowIfContainsExpiredAccessToken(HttpContext)"/>/>
    /// </summary>
    public static bool ContainsExpiredAccessToken(this HttpContext context)
    {
        return context.User.IsAuthenticated() is false && context.Request.Headers.Authorization.Any() is true;
    }
}

