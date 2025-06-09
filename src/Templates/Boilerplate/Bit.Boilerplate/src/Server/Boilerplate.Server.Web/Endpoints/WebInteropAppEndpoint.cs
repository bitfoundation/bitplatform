using Microsoft.AspNetCore.Mvc;
using Boilerplate.Shared.Attributes;
using Boilerplate.Client.Core.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Server.Web.Endpoints;

public static partial class WebInteropAppEndpoint
{
    public static WebApplication UseWebInteropApp(this WebApplication app)
    {
        app.MapGet("/web-interop-app", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)] async ([FromServices] HtmlRenderer renderer, HttpContext context) =>
        {
            // For more details, Check out WebInteropApp's comments.
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
                (await renderer.RenderComponentAsync<WebInteropApp>()).ToHtmlString());
            return Results.Content(html, "text/html");
        }).CacheOutput("AppResponseCachePolicy").WithTags("WebInteropApp");

        return app;
    }
}
