using Microsoft.AspNetCore.OutputCaching;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

namespace Boilerplate.Server.Web.Services;

public partial class BlazorOutputCachePolicy : IOutputCachePolicy
{
    [AutoInject] private ILogger<BlazorOutputCachePolicy> logger;
    [AutoInject] private IHostEnvironment env;

    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var blazorCache = context.HttpContext.GetBlazorCache();

        if (blazorCache is null)
        {
            context.EnableOutputCaching = false;
            return;
        }

        if (env.IsDevelopment())
        {
            var requestUrl = context.HttpContext.Request.GetUri()?.PathAndQuery;
            logger.LogInformation("In production, the HTML result of {Url} url would be cached.", requestUrl);
            return; // To enhance the developer experience, return here to make it easier for developers to debug cacheable pages.
        }

        context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(blazorCache.Duration);
        context.Tags.Add(new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery);
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
