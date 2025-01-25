using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Api.Services;

internal class AppResponseCachePolicy(IHostEnvironment env, ILogger<AppResponseCachePolicy> logger) : IOutputCachePolicy
{
    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();

        if (responseCacheAtt is null || context.HttpContext.User.IsAuthenticated() is true)
        {
            context.EnableOutputCaching = false;
            return;
        }

        var requestUrl = new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery;

        if (env.IsDevelopment())
        {
            context.EnableOutputCaching = false;
            logger.LogInformation("In production, the result response of {Url} url would be cached.", requestUrl);
            return; // To enhance the developer experience, return here to make it easier for developers to debug cacheable pages.
        }

        var duration = TimeSpan.FromSeconds(responseCacheAtt.MaxAge > 0 ? responseCacheAtt.MaxAge : responseCacheAtt.SharedMaxAge);

        context.ResponseExpirationTimeSpan = duration;
        context.Tags.Add(requestUrl);

        context.HttpContext.Response.GetTypedHeaders().CacheControl = new()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(responseCacheAtt.MaxAge),
            SharedMaxAge = TimeSpan.FromSeconds(responseCacheAtt.SharedMaxAge)
        };
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
