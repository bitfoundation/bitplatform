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
        // 1. Priority: Header (Explicit & CSRF-safe)
        // We check the Authorization header first. If a client (e.g., Blazor Hybrid App)
        // explicitly sends a token, it takes precedence over any potentially stale or unrelated cookies.
        // This aligns with the 'AutoCsrfProtectionFilter' logic, which treats header-based requests as secure.
        string? authHeader = context.Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authHeader) is false && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        // 2. Priority: Cookie (Implicit & requires CSRF checks)
        // If no header is found, we fall back to the cookie.
        // This is typically used for standard web browser clients.
        if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
        {
            return cookieToken;
        }

        return null;
    }
}

