//+:cnd:noEmit
using System.Reflection;
using Boilerplate.Shared.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    internal static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.OfType<AppResponseCacheAttribute>().FirstOrDefault() is AppResponseCacheAttribute attr) // minimal api
        {
            if (attr is not null)
            {
                attr.ResourceKind = ResourceKind.Api;
                return attr;
            }
        }

        if (context.GetEndpoint()?.Metadata.OfType<ComponentTypeMetadata>().FirstOrDefault() is ComponentTypeMetadata component) // razor page
        {
            var att = component.Type.GetCustomAttribute<AppResponseCacheAttribute>(inherit: true);
            if (att is not null)
            {
                att.ResourceKind = ResourceKind.Page;
                return att;
            }
        }

        //#if (IsInsideProjectTemplate)
        if (context.GetEndpoint()?.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault() is ControllerActionDescriptor action) // web api mvc action
        {
            var att = action.MethodInfo.GetCustomAttribute<AppResponseCacheAttribute>(inherit: true) ??
                action.ControllerTypeInfo.GetCustomAttribute<AppResponseCacheAttribute>(inherit: true);

            if (att is not null)
            {
                att.ResourceKind = ResourceKind.Api;
                return att;
            }
        }
        //#endif

        return null;
    }
}
