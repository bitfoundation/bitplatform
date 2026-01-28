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
}
