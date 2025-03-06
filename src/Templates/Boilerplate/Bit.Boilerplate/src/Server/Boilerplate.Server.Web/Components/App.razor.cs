using Microsoft.AspNetCore.Components;
using Boilerplate.Client.Core.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;

namespace Boilerplate.Server.Web.Components;

public partial class App
{
    private static readonly IComponentRenderMode noPrerenderBlazorWebAssembly = new InteractiveWebAssemblyRenderMode(prerender: false);
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;

    [AutoInject] ServerWebSettings serverWebSettings = default!;
    [AutoInject] IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] AbsoluteServerAddressProvider absoluteServerAddress = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (CultureInfoManager.MultilingualEnabled)
        {
            HttpContext?.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                                                 CookieRequestCultureProvider.MakeCookieValue(new(CultureInfo.CurrentUICulture)));
        }
    }
}

/// <summary>
/// Streaming pre-rendering improves user experience (UX) and overall application performance. 
/// However, it prevents search engines from indexing pre-rendered dynamic content and is incompatible with response caching.  
/// To mitigate this, conditional logic in App.razor utilizes <see cref="HttpRequestExtensions.DisableStreamPrerendering"/>  
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
