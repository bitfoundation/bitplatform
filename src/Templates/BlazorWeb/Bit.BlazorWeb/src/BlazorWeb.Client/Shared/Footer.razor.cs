//-:cnd:noEmit
namespace BlazorWeb.Client.Shared;

public partial class Footer
{
    [AutoInject] private BitThemeManager bitThemeManager = default!;

    private BitDropdownItem<string>[] cultures = default!;

#if MultilingualEnabled
    protected override Task OnInitAsync()
    {
        cultures = CultureInfoManager.SupportedCultures
                                      .Select(sc => new BitDropdownItem<string> { Value = sc.code, Text = sc.name })
                                      .ToArray();

        SelectedCulture = CultureInfoManager.GetCurrentCulture();

        return base.OnInitAsync();
    }
#endif

    private string? SelectedCulture;

    private async Task OnCultureChanged()
    {
        if (RenderModeProvider.PrerenderEnabled is true)
        {
            var cultureCookie = $"c={SelectedCulture}|uic={SelectedCulture}";
            await JSRuntime.SetCookie(".AspNetCore.Culture", cultureCookie, expiresIn: 30 * 24 * 3600, rememberMe: true);
        }

        await StorageService.SetItem("Culture", SelectedCulture, persistent: true);

        NavigationManager.Refresh(forceReload: true);
    }

    private async Task ToggleTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }
}
