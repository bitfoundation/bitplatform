using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Boilerplate.Client.Core.Services;

namespace Boilerplate.Server.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (AppRenderMode.MultilingualEnabled)
        {
            HttpContext?.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new(CultureInfo.CurrentUICulture)));
        }
    }
}
