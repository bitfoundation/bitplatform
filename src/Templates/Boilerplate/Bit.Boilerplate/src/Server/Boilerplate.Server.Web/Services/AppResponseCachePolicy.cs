using Microsoft.AspNetCore.OutputCaching;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

namespace Boilerplate.Server.Web.Services;

public partial class AppResponseCachePolicy : IOutputCachePolicy
{
    [AutoInject] private IHostEnvironment env;
    [AutoInject] private ILogger<AppResponseCachePolicy> logger;

    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();

        if (responseCacheAtt is null || context.HttpContext.User.IsAuthenticated() is true)
        {
            context.EnableOutputCaching = false;
            return;
        }

        var requestUrl = new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery;

        if (env.IsDevelopment() && false)
        {
            context.EnableOutputCaching = false;
            logger.LogInformation("In production, the result response of {Url} url would be cached.", requestUrl);
            return; // To enhance the developer experience, return here to make it easier for developers to debug cacheable pages.
        }

        var duration = TimeSpan.FromSeconds(responseCacheAtt.MaxAge > 0 ? responseCacheAtt.MaxAge : responseCacheAtt.SharedMaxAge);

        context.ResponseExpirationTimeSpan = duration;
        context.Tags.Add(requestUrl);
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
