using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmPlatforms
{
    private static readonly (LandingPlatform Platform, string Icon, string Detail)[] rows =
    [
        (LandingPlatform.Web, BitIconName.Globe, nameof(CrmStrings.AppsWebDetail)),
        (LandingPlatform.Windows, BitIconName.WindowsLogo, nameof(CrmStrings.AppsWindowsDetail)),
        (LandingPlatform.macOS, BitIconName.LaptopSelected, nameof(CrmStrings.AppsMacDetail)),
        (LandingPlatform.iOS, BitIconName.MobileSelected, nameof(CrmStrings.AppsIosDetail)),
        (LandingPlatform.Android, BitIconName.CellPhone, nameof(CrmStrings.AppsAndroidDetail))
    ];

    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private LandingPlatform? visitorPlatform;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var userAgent = await navigator.GetUserAgent();

            visitorPlatform = userAgent switch
            {
                _ when userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) => LandingPlatform.iOS,
                _ when userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) => LandingPlatform.Android,
                _ when userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) => LandingPlatform.Windows,
                _ when userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) => LandingPlatform.macOS,
                _ => null
            };

            if (visitorPlatform is not null)
            {
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
