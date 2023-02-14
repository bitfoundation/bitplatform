//-:cnd:noEmit
namespace BlazorWeb.Web;

public partial class Footer
{
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

    private static List<BitDropDownItem> GetCultures() =>
        CultureInfoManager.SupportedCultures.Select(sc => new BitDropDownItem { Value = sc.code, Text = sc.name }).ToList();
}
