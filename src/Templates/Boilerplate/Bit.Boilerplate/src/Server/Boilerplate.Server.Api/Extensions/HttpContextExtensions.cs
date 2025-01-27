//+:cnd:noEmit

namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    internal static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.OfType<AppResponseCacheAttribute>().FirstOrDefault() is AppResponseCacheAttribute attr)
        {
            if (attr is not null)
            {
                attr.ResourceKind = ResourceKind.Api;
                return attr;
            }
        }

        return null;
    }
}

