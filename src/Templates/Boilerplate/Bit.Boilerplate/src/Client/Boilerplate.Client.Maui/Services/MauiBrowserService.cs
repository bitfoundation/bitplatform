namespace Boilerplate.Client.Maui.Services;
public class MauiBrowserService : IBrowserService
{
    public async Task OpenUrl(string url)
    {
        await Browser.OpenAsync(url, BrowserLaunchMode.External);
    }
}
