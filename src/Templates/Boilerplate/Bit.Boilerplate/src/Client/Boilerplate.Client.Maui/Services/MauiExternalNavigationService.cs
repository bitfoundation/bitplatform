namespace Boilerplate.Client.Maui.Services;

public partial class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task NavigateToAsync(string url)
    {
        await Browser.OpenAsync(url, AppPlatform.IsWindows || AppPlatform.IsMacOS ? BrowserLaunchMode.External : BrowserLaunchMode.SystemPreferred /* in app browser */);
    }
}
