using Boilerplate.Client.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;

namespace Boilerplate.Server.Web.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;

    [AutoInject] WebAppRenderMode webAppRenderMode = default!;
    [AutoInject] IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] ServerWebAppSettings serverWebAppSettings = default!;

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
