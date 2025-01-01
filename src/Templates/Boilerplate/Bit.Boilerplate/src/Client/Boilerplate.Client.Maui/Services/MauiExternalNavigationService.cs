using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task NavigateToAsync(string url)
    {
        var isDark = Application.Current!.UserAppTheme == AppTheme.Dark;

        await Browser.OpenAsync(url, options: new()
        {
            TitleMode = BrowserTitleMode.Hide,
            PreferredToolbarColor = Color.Parse(isDark ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor),
            LaunchMode = AppPlatform.IsWindows || AppPlatform.IsMacOS ? BrowserLaunchMode.External : BrowserLaunchMode.SystemPreferred /* in app browser */
        });
    }
}
