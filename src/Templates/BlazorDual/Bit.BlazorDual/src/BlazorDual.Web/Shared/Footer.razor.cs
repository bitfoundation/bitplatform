//-:cnd:noEmit
namespace BlazorDual.Web.Shared;

public partial class Footer
{
    [AutoInject] private BitThemeManager _bitThemeManager { get; set; } = default!;

#if MultilingualEnabled
    protected async override Task OnAfterFirstRenderAsync()
    {
        var preferredCultureCookie = await JSRuntime.InvokeAsync<string?>("window.App.getCookie", ".AspNetCore.Culture");

        SelectedCulture = CultureInfoManager.GetCurrentCulture(preferredCultureCookie);

        StateHasChanged();

        await base.OnAfterFirstRenderAsync();
    }
#endif

    private string? SelectedCulture;

    private async Task OnCultureChanged()
    {
        var cultureCookie = $"c={SelectedCulture}|uic={SelectedCulture}";

        await JSRuntime.InvokeVoidAsync("window.App.setCookie", ".AspNetCore.Culture", cultureCookie, 30 * 24 * 3600);

        NavigationManager.ForceReload();
    }

    private static List<BitDropdownItem<string>> GetCultures() =>
        CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.code, Text = sc.name }).ToList();

    private async Task ToggleTheme()
    {
        await _bitThemeManager.ToggleDarkLightAsync();
    }
}
