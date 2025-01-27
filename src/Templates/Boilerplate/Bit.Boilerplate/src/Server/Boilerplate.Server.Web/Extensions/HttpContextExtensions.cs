//+:cnd:noEmit
using Boilerplate.Shared.Attributes;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    internal static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.OfType<AppResponseCacheAttribute>().FirstOrDefault() is AppResponseCacheAttribute attr)
        {
            if (attr is not null)
            {
                return attr;
            }
        }

        return null;
    }

    internal static bool IsBlazorPageContext(this HttpContext context)
    {
        return context.GetEndpoint()?.Metadata?.OfType<ComponentTypeMetadata>()?.Any() is true;
    }
}
