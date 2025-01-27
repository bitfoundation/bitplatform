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
                var isPageRequest = context.GetEndpoint()?.Metadata.OfType<ComponentTypeMetadata>().FirstOrDefault() is ComponentTypeMetadata;
                attr.ResourceKind = isPageRequest ? ResourceKind.Page : ResourceKind.Api;
                return attr;
            }
        }

        return null;
    }
}
