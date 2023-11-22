//-:cnd:noEmit
namespace BlazorWeb.Client.Shared;

public partial class Footer
{
    [AutoInject] private BitThemeManager _bitThemeManager = default!;

    private BitDropdownItem<string>[] _cultures = default!;

    protected override Task OnInitAsync()
    {
        _cultures = CultureInfoManager.SupportedCultures
                                      .Select(sc => new BitDropdownItem<string> { Value = sc.code, Text = sc.name })
                                      .ToArray();
        return base.OnInitAsync();
    }

#if MultilingualEnabled
    protected async override Task OnAfterFirstRenderAsync()
    {
        var preferredCultureCookie = await JSRuntime.GetCookie(".AspNetCore.Culture");

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
        await _bitThemeManager.ToggleDarkLightAsync();
    }
}
