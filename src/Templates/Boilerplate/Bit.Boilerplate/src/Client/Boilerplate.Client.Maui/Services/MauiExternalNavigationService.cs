namespace Boilerplate.Client.Maui.Services;

public class MauiExternalNavigationService : IExternalNavigationService
{
    public async Task NavigateToAsync(string url)
    {
        await Browser.OpenAsync(url, OperatingSystem.IsAndroid() ? BrowserLaunchMode.SystemPreferred : BrowserLaunchMode.External);
    }
}
