using System.Reflection;
using Boilerplate.Client.Core.Components;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    public static BlazorOutputCacheAttribute? GetBlazorCache(this HttpContext context)
    {
        var componentMetadata = context.GetEndpoint()?.Metadata.OfType<ComponentTypeMetadata>();

        if (componentMetadata?.FirstOrDefault() is not ComponentTypeMetadata component)
            return null;

        return component.Type.GetCustomAttribute<BlazorOutputCacheAttribute>(inherit: true);
    }
}
