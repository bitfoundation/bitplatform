namespace Boilerplate.Client.Maui.Services;

public class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task NavigateToAsync(string url)
    {
        await Browser.OpenAsync(url, PlatformInfo.IsAndroid ? BrowserLaunchMode.SystemPreferred : BrowserLaunchMode.External);
    }
}
