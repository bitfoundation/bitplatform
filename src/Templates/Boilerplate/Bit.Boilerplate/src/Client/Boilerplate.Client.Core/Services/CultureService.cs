namespace Boilerplate.Client.Core.Services;

public partial class CultureService
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;

    public async Task ChangeCulture(string? cultureName)
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            await storageService.SetItem("Culture", cultureName, persistent: true);
            cultureInfoManager.SetCurrentCulture(cultureName!);
            pubSubService.Publish(ClientPubSubMessages.CULTURE_CHANGED, cultureName);
        }
        else
        {
            await cookie.Set(new()
            {
                MaxAge = 30 * 24 * 3600,
                Name = ".AspNetCore.Culture",
                Secure = AppEnvironment.IsDev() is false,
                Value = Uri.EscapeDataString($"c={cultureName}|uic={cultureName}"),
            });
        }

        navigationManager.NavigateTo(new Uri(navigationManager.Uri).GetUrlWithoutCulture(), forceLoad: true, replace: true);
    }
}
