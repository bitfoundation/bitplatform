//+:cnd:noEmit
namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
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

    public static string? GetAccessToken(this HttpContext context)
    {
        return context.Request.Query.ContainsKey("access_token") ? context.Request.Query["access_token"]
            : context.Request.Cookies.ContainsKey("access_token") ? context.Request.Cookies["access_token"]
            : context.Request.Headers.TryGetValue("Authorization", out var value) ? value.ToString().Replace("Bearer ", "")
            : null;
    }
}

