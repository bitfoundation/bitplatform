using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;

namespace Boilerplate.Server.Web.Components;

public partial class App
{
    [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;

    [AutoInject] ServerWebSettings serverWebSettings = default!;
    [AutoInject] AbsoluteServerAddressProvider absoluteServerAddress = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Not written when the response is headed for a shared cache: a Set-Cookie of ANY name makes a CDN refuse to
        // cache the whole response (Cloudflare answers cf-cache-status: BYPASS), which would cost every pre-rendered
        // page its edge entry. The client persists the same cookie right after boot instead - See
        // CultureService.PersistCurrentCulture.
        if (CultureInfoManager.InvariantGlobalization is false && HttpContext?.IsSharedResponseCacheEnabled() is false)
        {
            HttpContext.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                                                CookieRequestCultureProvider.MakeCookieValue(new(CultureInfo.CurrentUICulture)),
                                                new() { IsEssential = true });
        }
    }
}

/// <summary>
/// Streaming pre-rendering improves user experience (UX) and overall application performance. 
/// However, it prevents search engines from indexing pre-rendered dynamic content and is incompatible with response caching.  
/// To mitigate this, conditional logic in App.razor utilizes <see cref="HttpRequestExtensions.IsStreamPrerenderingSuppressed"/>  
/// to disable streaming in those scenarios.
/// </summary>
[StreamRendering(enabled: true)]
public class StreamRenderingEnabledContainer : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}

/// <summary>
/// <inheritdoc cref="StreamRenderingEnabledContainer"/>
/// </summary>
[StreamRendering(enabled: false)]
public class StreamRenderingDisabledContainer : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}
