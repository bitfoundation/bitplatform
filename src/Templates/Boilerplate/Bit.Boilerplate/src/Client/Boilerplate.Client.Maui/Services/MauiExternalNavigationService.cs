namespace Boilerplate.Client.Maui.Services;

public partial class MauiExternalNavigationService : IExternalNavigationService
{
    public static bool ShowExternalBrowser => AppPlatform.IsWindows || AppPlatform.IsMacOS;

    public async Task NavigateToAsync(string url)
    {
        await Browser.OpenAsync(url, ShowExternalBrowser ? BrowserLaunchMode.External : BrowserLaunchMode.SystemPreferred /* in app browser */);
    }
}
