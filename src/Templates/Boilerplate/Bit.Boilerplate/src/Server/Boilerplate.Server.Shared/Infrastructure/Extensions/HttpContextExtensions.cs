//+:cnd:noEmit
using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    public static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        var att = context.GetEndpoint()?.Metadata.OfType<AppResponseCacheAttribute>().FirstOrDefault();

        if (att?.MaxAge == -1 && att?.SharedMaxAge == -1)
            throw new InvalidOperationException("Invalid configuration: Both MaxAge and SharedMaxAge are unset. At least one of them must be specified in the ResponseCache attribute.");

        return att;
    }

    public static bool IsBlazorPageContext(this HttpContext context)
    {
        return context.GetEndpoint()?.Metadata?.OfType<ComponentTypeMetadata>()?.Any() is true;
    }

    /// <summary>
    /// Is CDN Edge or ASP.NET Core Output cache enabled for this response.
    /// </summary>
    public static bool IsSharedResponseCacheEnabled(this HttpContext context)
    {
        if (context.Items.TryGetValue("AppResponseCachePolicy__SharedCacheEnabled", out var val) && val is true)
            return true;

        return false;
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
