using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Server.Api.Controllers.HybridAppWebInterop;

[ApiController, AllowAnonymous]
[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
public partial class HybridAppWebInteropController : ControllerBase
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    /// <summary>
    /// API that opens web features that are not available in the blazor hybrid's webview such as social sign-in etc.
    /// </summary>
    [HttpGet]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
    public async Task<ActionResult> WebAppInterop()
    {
        // For more details, checkout HybridAppWebInteropPage's comments.

        var appJSUrl = new Uri(Request.GetWebAppUrl(), "_content/Boilerplate.Client.Core/scripts/app.js").ToString();

        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            (await htmlRenderer.RenderComponentAsync<HybridAppWebInteropPage>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(HybridAppWebInteropPage.AppJSUrl), appJSUrl }
            }))).ToHtmlString());

        return Content(html, "text/html");
    }
}
