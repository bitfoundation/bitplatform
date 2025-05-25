using Boilerplate.Shared.Attributes;
using Boilerplate.Client.Core.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Server.Web.Endpoints;

public static partial class HybridWebAppInteropEndpoint
{
    public static WebApplication UseHybridWebAppInterop(this WebApplication app)
    {
        app.MapGet("/hybrid-app-web-interop", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)] async ([FromServices] HtmlRenderer renderer, HttpContext context) =>
        {
            // For more details, Check out HybridAppWebInterop's comments.
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<HybridAppWebInterop>()).ToHtmlString());
            return Results.Content(html, "text/html");
        }).CacheOutput("AppResponseCachePolicy").WithTags("HybridWebAppInterop");

        return app;
    }
}
