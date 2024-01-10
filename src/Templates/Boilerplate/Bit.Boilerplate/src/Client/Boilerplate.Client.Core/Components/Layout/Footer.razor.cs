//-:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Layout;

public partial class Footer
{
    [AutoInject] private Cookie cookie = default!;
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
        await cookie.Set(new()
        {
            Name = ".AspNetCore.Culture",
            Value = $"c={SelectedCulture}|uic={SelectedCulture}",
            MaxAge = 30 * 24 * 3600,
            Secure = BuildConfiguration.IsRelease()
        });

        await StorageService.SetItem("Culture", SelectedCulture, persistent: true);

        // Relevant in the context of Blazor Hybrid, where the reloading of the web view doesn't result in the resetting of all static in memory data on the client side
        CultureInfoManager.SetCurrentCulture(SelectedCulture);

        NavigationManager.Refresh(forceReload: true);
    }

    private async Task ToggleTheme()
    {
        await bitDeviceCoordinator.ApplyTheme(await bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
