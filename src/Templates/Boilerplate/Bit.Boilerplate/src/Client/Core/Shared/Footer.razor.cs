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

        SelectedCulture = CultureInfoManager.GetCurrentCulture();

        return base.OnInitAsync();
    }

    private string? SelectedCulture;

    private async Task OnCultureChanged()
    {
        if (WebAppDeploymentTypeDetector.Current.IsPrerenderEnabled() && BlazorModeDetector.Current.IsBlazorHybrid() is false)
        {
            var cultureCookie = $"c={SelectedCulture}|uic={SelectedCulture}";
            await JSRuntime.SetCookie(".AspNetCore.Culture", cultureCookie, expiresIn: 30 * 24 * 3600, rememberMe: true);
        }

        await StorageService.SetItem("Culture", SelectedCulture, persistent: true);

        NavigationManager.Refresh(forceReload: true);
    }

    private async Task ToggleTheme()
    {
        await bitDeviceCoordinator.ApplyTheme(await bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
