namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class CultureService
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private NavigationManager navigationManager = default!;

    public async Task ChangeCulture(string? cultureName)
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            await storageService.SetItem("Culture", cultureName, persistent: true);
            CultureInfoManager.SetCurrentCulture(cultureName);
            pubSubService.Publish(ClientAppMessages.CULTURE_CHANGED, cultureName);
        }
        else
        {
            await cookie.Set(new()
            {
                Name = ".AspNetCore.Culture",
                Value = Uri.EscapeDataString($"c={cultureName}|uic={cultureName}"),
                MaxAge = 3600 * 24 * 30,
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDevelopment() is false
            });
        }

        navigationManager.NavigateTo(new Uri(navigationManager.Uri).GetUrlWithoutCulture(), forceLoad: true, replace: true);
    }
}
