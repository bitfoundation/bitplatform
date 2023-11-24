//-:cnd:noEmit
namespace Boilerplate.Client.Core.Shared;

public partial class Footer
{
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator { get; set; } = default!;

    private BitDropdownItem<string>[] cultures = default!;

    protected override Task OnInitAsync()
    {
        cultures = CultureInfoManager.SupportedCultures
                                      .Select(sc => new BitDropdownItem<string> { Value = sc.code, Text = sc.name })
                                      .ToArray();
        return base.OnInitAsync();
    }


#if MultilingualEnabled
    protected override async Task OnAfterFirstRenderAsync()
    {
#if BlazorHybrid
        var preferredCultureCookie = Preferences.Get(".AspNetCore.Culture", null);
#else
        var preferredCultureCookie = await JSRuntime.GetCookie(".AspNetCore.Culture");
#endif
        SelectedCulture = CultureInfoManager.GetCurrentCulture(preferredCultureCookie);

        StateHasChanged();

        await base.OnAfterFirstRenderAsync();
    }
#endif

    private string? SelectedCulture;

    private async Task OnCultureChanged()
    {
        var cultureCookie = $"c={SelectedCulture}|uic={SelectedCulture}";

        await JSRuntime.SetCookie(".AspNetCore.Culture", cultureCookie, 30 * 24 * 3600, rememberMe: true);

        NavigationManager.Refresh(forceReload: true);
    }

    private async Task ToggleTheme()
    {
        await bitDeviceCoordinator.ApplyTheme(await bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
