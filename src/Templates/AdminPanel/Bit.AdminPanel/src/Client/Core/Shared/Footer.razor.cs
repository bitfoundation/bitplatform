//-:cnd:noEmit

namespace AdminPanel.Client.Core.Shared;

public partial class Footer
{
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
#if BlazorHybrid
        var preferredCultureCookie = Preferences.Get(".AspNetCore.Culture", null);
#else
        var preferredCultureCookie = await JsRuntime.InvokeAsync<string?>("window.App.getCookie", ".AspNetCore.Culture");
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
        await JsRuntime.InvokeVoidAsync("window.App.setCookie", ".AspNetCore.Culture", cultureCookie, 30 * 24 * 3600);
#endif

        NavigationManager.ForceReload();
    }
}
