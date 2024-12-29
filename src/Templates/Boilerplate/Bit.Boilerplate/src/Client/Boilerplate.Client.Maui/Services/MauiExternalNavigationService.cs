namespace Boilerplate.Client.Maui.Services;

public partial class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task NavigateToAsync(string url)
    {
        await Browser.OpenAsync(url, AppPlatform.IsAndroid || AppPlatform.IsIOS ? BrowserLaunchMode.SystemPreferred : BrowserLaunchMode.External /*Windows,macOS*/);
    }
}
