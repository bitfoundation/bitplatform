namespace Boilerplate.Client.Core.Services;

public partial class DefaultExternalNavigationService : IExternalNavigationService
{
    [AutoInject] private readonly Window window = default!;
    [AutoInject] private readonly NavigationManager navigationManager = default!;

    public async Task NavigateToAsync(string url)
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            navigationManager.NavigateTo(url, forceLoad: true, replace: true);
            return;
        }

        if (await window.Open(url, "_blank", new WindowFeatures() { Popup = true, Height = 768, Width = 1024 }) is false // Let's try with popup first.
            && await window.Open(url, "_blank", new WindowFeatures() { Popup = false }) is false) // Let's try new tab
        {
            navigationManager.NavigateTo(url, forceLoad: true, replace: true); // If all else fails, let's try to navigate in the same tab.
        }
    }
}
