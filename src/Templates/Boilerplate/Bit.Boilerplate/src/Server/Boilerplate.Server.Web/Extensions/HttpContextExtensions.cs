//+:cnd:noEmit
using Boilerplate.Shared.Attributes;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    internal static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        return context.GetEndpoint()?.Metadata.OfType<AppResponseCacheAttribute>().FirstOrDefault();
    }

    internal static bool IsBlazorPageContext(this HttpContext context)
    {
        return context.GetEndpoint()?.Metadata?.OfType<ComponentTypeMetadata>()?.Any() is true;
    }
}
