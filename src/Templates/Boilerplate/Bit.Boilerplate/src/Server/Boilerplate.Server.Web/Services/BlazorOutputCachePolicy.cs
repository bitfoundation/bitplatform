using Microsoft.AspNetCore.OutputCaching;
using Boilerplate.Client.Core.Components.Pages;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Boilerplate.Server.Web.Services;

public class BlazorOutputCachePolicy : IOutputCachePolicy
{
    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var componentMetadata = context.HttpContext.GetEndpoint()?.Metadata.OfType<ComponentTypeMetadata>();

        if (componentMetadata?.FirstOrDefault() is not ComponentTypeMetadata component)
            return;

        context.Tags.Add(component.Type.Name);
        context.EnableOutputCaching = component.Type == typeof(TermsPage);
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
