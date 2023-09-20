//-:cnd:noEmit
namespace TodoTemplate.Client.Core.Shared;

public partial class Footer
{
    [AutoInject] private BitThemeManager _bitThemeManager { get; set; } = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;

#if MultilingualEnabled
    protected async override Task OnAfterFirstRenderAsync()
    {
#if BlazorHybrid
        var preferredCultureCookie = Preferences.Get(".AspNetCore.Culture", null);
#else
        var preferredCultureCookie = await JSRuntime.InvokeAsync<string?>("window.App.getCookie", ".AspNetCore.Culture");
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

#if BlazorHybrid
        Preferences.Set(".AspNetCore.Culture", cultureCookie);
#else
        await JSRuntime.InvokeVoidAsync("window.App.setCookie", ".AspNetCore.Culture", cultureCookie, 30 * 24 * 3600);
#endif

        NavigationManager.Refresh(forceReload: true);
    }

    private static List<BitDropdownItem<string>> GetCultures() =>
        CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.code, Text = sc.name }).ToList();

    private async Task ToggleTheme()
    {
        await _bitDeviceCoordinator.SetDeviceTheme(await _bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
