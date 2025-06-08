namespace Boilerplate.Client.Core.Services;

public partial class DefaultExternalNavigationService : IExternalNavigationService
{
    [AutoInject] private readonly Window window = default!;
    [AutoInject] private readonly PubSubService pubSubService = default!;
    [AutoInject] private readonly NavigationManager navigationManager = default!;

    /// <summary>
    /// The MauiExternalNavigationService (Client.Maui) implementation of <see cref="IExternalNavigationService"/> can show one window at a time
    /// on Android and iOS apps. Trying to have similar UX across platforms, we close the last opened window before opening the new one in web platform as well.
    /// </summary>
    private string? lastOpenedWindowId = null;
    private Action? pubSubUnsubscribe;

    public async Task NavigateToAsync(string url)
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            // Client.Windows:
            navigationManager.NavigateTo(url, forceLoad: true, replace: true);
            return;
        }

        // Client.Web:
        if (lastOpenedWindowId is not null)
        {
            await window.Close(lastOpenedWindowId); // Only one open window at a time.
        }

        pubSubUnsubscribe?.Invoke();
        pubSubUnsubscribe = pubSubService.Subscribe(ClientPubSubMessages.SOCIAL_SIGN_IN, async _ =>
        {
            await window.Close(lastOpenedWindowId); // It's time to close the social sign-in popup window.
        });

        if ((lastOpenedWindowId = await window.Open(url, "_blank", new WindowFeatures() { Popup = true, Width = 1024, Height = 768 })) is null // Let's try with popup first.
        && (lastOpenedWindowId = await window.Open(url, "_blank", new WindowFeatures() { Popup = false })) is null) // Let's try new tab
        {
            navigationManager.NavigateTo(url, forceLoad: true, replace: true); // If all else fails, let's try to navigate in the same tab.
        }
    }
}
