using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Maui.Infrastructure.Services;

public partial class MauiExternalNavigationService : IExternalNavigationService
{
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;

    public async Task NavigateTo(string url)
    {
        // A private-use scheme (vscode://) belongs to another app on this device and a browser cannot open it; Launcher
        // asks the OS, and says when nothing handles it - or the caller reports a hand-off that never happened.
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is not ("http" or "https"))
        {
            if (await Launcher.OpenAsync(uri) is false)
                throw new DomainLogicException(localizer["No application on this device can open {0} links.", uri.Scheme]);

            return;
        }

        var isDark = Application.Current!.UserAppTheme == AppTheme.Dark;

        await Browser.OpenAsync(url, options: new()
        {
            TitleMode = BrowserTitleMode.Hide,
            PreferredToolbarColor = Color.Parse(isDark ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor),
            LaunchMode = AppPlatform.IsWindows || AppPlatform.IsMacOS ? BrowserLaunchMode.External : BrowserLaunchMode.SystemPreferred /* in app browser */
        });
    }
}
