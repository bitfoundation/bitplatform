namespace Boilerplate.Client.Core;

public partial class Routes
{
    [AutoInject] NavigationManager? navigationManager { set => universalLinksNavigationManager = value; get => universalLinksNavigationManager; }
    public static NavigationManager? universalLinksNavigationManager;

    public static async Task OpenUniversalLink(string url, bool forceLoad = false, bool replace = false)
    {
        await Task.Run(async () =>
        {
            while (universalLinksNavigationManager is null)
            {
                await Task.Yield();
            }
        });

        universalLinksNavigationManager!.NavigateTo(url, forceLoad, replace);
    }
}
