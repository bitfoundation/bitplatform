namespace Boilerplate.Client.Core.Services;

public class AppBrowserService : IBrowserService
{
    public async Task OpenUrl(string url)
    {
        await Routes.OpenUniversalLink(url, forceLoad: true, replace: true);
    }
}
