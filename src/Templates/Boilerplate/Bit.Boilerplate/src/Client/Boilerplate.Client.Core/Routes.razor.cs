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

        if (CultureInfoManager.MultilingualEnabled && forceLoad == false)
        {
            var currentCulture = CultureInfo.CurrentUICulture.Name;
            var uri = new Uri(url);
            var urlCulture = uri.GetCulture();
            forceLoad = urlCulture is not null && currentCulture != urlCulture;
        }

        universalLinksNavigationManager!.NavigateTo(url, forceLoad, replace);
    }
}
