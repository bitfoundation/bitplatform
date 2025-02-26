namespace Boilerplate.Client.Core.Services;

public partial class DefaultExternalNavigationService : IExternalNavigationService
{
    [AutoInject] private readonly Window window = default!;
    [AutoInject] private readonly NavigationManager navigationManager = default!;

    /// <summary>
    /// The MauiExternalNavigationService (Client.Maui) implementation of <see cref="IExternalNavigationService"/> can show one window at a time
    /// on Android and iOS apps. Trying to have similar UX across platforms, we close the last opened window before opening the new one in web platform as well.
    /// </summary>
    private string? lastOpenedWindow = null;

    public async Task NavigateToAsync(string url)
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            // Client.Windows:
            navigationManager.NavigateTo(url, forceLoad: true, replace: true);
            return;
        }


        // Client.Web:
        if (lastOpenedWindow is not null)
        {
            await window.Close(lastOpenedWindow);
        }

        if ((lastOpenedWindow = await window.Open(url, "_blank", new WindowFeatures() { Popup = true, Height = 768, Width = 1024 })) is null // Let's try with popup first.
            && (lastOpenedWindow = await window.Open(url, "_blank", new WindowFeatures() { Popup = false })) is null) // Let's try new tab
        {
            navigationManager.NavigateTo(url, forceLoad: true, replace: true); // If all else fails, let's try to navigate in the same tab.
        }
    }
}
