//+:cnd:noEmit
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.Http;

internal static class HttpContextExtensions
{
    internal static AppResponseCacheAttribute? GetResponseCacheAttribute(this HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault() is ControllerActionDescriptor action)
        {
            var att = action.MethodInfo.GetCustomAttribute<AppResponseCacheAttribute>(inherit: true) ??
                action.ControllerTypeInfo.GetCustomAttribute<AppResponseCacheAttribute>(inherit: true);

            if (att is not null)
            {
                att.ResourceKind = ResourceKind.Api;
                return att;
            }
        }

        return null;
    }
}
