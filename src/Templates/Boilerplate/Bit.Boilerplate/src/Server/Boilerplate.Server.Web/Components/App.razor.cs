using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Localization;

namespace Boilerplate.Server.Web.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    private static readonly IComponentRenderMode noPrerenderBlazorWebAssembly = new InteractiveWebAssemblyRenderMode(prerender: false);

    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;

    [AutoInject] IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] ServerWebSettings serverWebSettings = default!;

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
