namespace TodoTemplate.App.Components;

public partial class Footer
{
#if MultilingualEnabled
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        try
        {
            if (firstRender)
            {
                var preferredCulture = await JSRuntime.InvokeAsync<string?>("window.App.getCookie", ".AspNetCore.Culture");
                SelectedCulture = CultureInfoManager.GetCurrentCulture(preferredCulture);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }
#endif

    string? SelectedCulture;

    async Task OnCultureChanged()
    {
        var cultureCookie = $"c={SelectedCulture}|uic={SelectedCulture}";

        await JSRuntime.InvokeVoidAsync("window.App.setCookie", ".AspNetCore.Culture", cultureCookie, 30 * 24 * 3600);

        NavigationManager.ForceReload();
    }

    List<BitDropDownItem> GetCultures()
    {
        return CultureInfoManager.SupportedCultures
            .Select(sc => new BitDropDownItem { Value = sc.code, Text = sc.name })
            .ToList();
    }
}
