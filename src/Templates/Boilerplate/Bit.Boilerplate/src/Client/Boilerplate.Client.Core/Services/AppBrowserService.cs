namespace Boilerplate.Client.Core.Services;

public partial class AppBrowserService : IBrowserService
{
    [AutoInject] private NavigationManager navigationManager = default!;

    public async Task OpenUrl(string url)
    {
        navigationManager.NavigateTo(url, forceLoad: true, replace: true);
    }
}
