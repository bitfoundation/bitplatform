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
}

