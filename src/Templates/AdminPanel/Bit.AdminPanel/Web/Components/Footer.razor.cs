using AdminPanel.App.Extensions;

namespace AdminPanel.App.Components;

public partial class Footer
{
#if MultilingualEnabled || true

    protected async override Task OnAfterFirstRenderAsync()
    {
#if Maui
        var preferredCultureCookie = Preferences.Get(".AspNetCore.Culture", null);
#else
        var preferredCultureCookie = await JsRuntime.InvokeAsync<string?>("window.App.getCookie", ".AspNetCore.Culture");
#endif
        SelectedCulture = CultureInfoManager.GetCurrentCulture(preferredCultureCookie);

        StateHasChanged();

        await base.OnAfterFirstRenderAsync();
    }
#endif

    string? SelectedCulture;

    async Task OnCultureChanged()
    {
        var cultureCookie = $"c={SelectedCulture}|uic={SelectedCulture}";

#if Maui
        Preferences.Set(".AspNetCore.Culture", cultureCookie);
#else
        await JsRuntime.InvokeVoidAsync("window.App.setCookie", ".AspNetCore.Culture", cultureCookie, 30 * 24 * 3600);
#endif

        NavigationManager.ForceReload();
    }

    List<BitDropDownItem> GetCultures()
    {
        return CultureInfoManager.SupportedCultures
            .Select(sc => new BitDropDownItem { Value = sc.code, Text = sc.name })
            .ToList();
    }
}
