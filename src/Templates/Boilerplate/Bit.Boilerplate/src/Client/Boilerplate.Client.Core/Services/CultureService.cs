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
                Name = ".AspNetCore.Culture",
                Value = Uri.EscapeDataString($"c={cultureName}|uic={cultureName}"),
                MaxAge = 30 * 24 * 3600,
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDev() is false
            });
        }

        navigationManager.NavigateTo(new Uri(navigationManager.Uri).GetUrlWithoutCulture(), forceLoad: true, replace: true);
    }
}
