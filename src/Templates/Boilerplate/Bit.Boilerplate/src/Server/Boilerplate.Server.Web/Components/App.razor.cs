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
/// Streaming pre-rendering enhances user experience (UX) and overall application performance. 
/// However, it prevents search engines from accessing your pre-rendered dynamic content.
/// To address this, the conditional logic in App.razor, leveraging <see cref="HttpRequestExtensions.IsCrawlerClient(HttpRequest)"/>, 
/// disables streaming specifically for search engine crawlers, while maintaining the improved UX and performance for regular users.
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
