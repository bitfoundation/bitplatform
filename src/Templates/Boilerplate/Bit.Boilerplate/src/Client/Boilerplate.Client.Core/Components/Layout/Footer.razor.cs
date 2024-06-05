//-:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Layout;

public partial class Footer
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IPubSubService pubSubService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;

    private BitDropdownItem<string>[] cultures = default!;

    protected override async Task OnInitAsync()
    {
        cultures = CultureInfoManager.SupportedCultures
                                      .Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                                      .ToArray();

        SelectedCulture = CultureInfo.CurrentUICulture.Name;

        await base.OnInitAsync();
    }

    private string? SelectedCulture;

    private async Task OnCultureChanged()
    {
        if (AppRenderMode.IsBlazorHybrid)
        {
            await StorageService.SetItem("Culture", SelectedCulture, persistent: true);
        }
        else
        {
            await cookie.Set(new()
            {
                Name = ".AspNetCore.Culture",
                Value = Uri.EscapeDataString($"c={SelectedCulture}|uic={SelectedCulture}"),
                MaxAge = 30 * 24 * 3600,
                Secure = BuildConfiguration.IsRelease()
            });
        }

        if (AppRenderMode.IsBlazorHybrid)
        {
            cultureInfoManager.SetCurrentCulture(SelectedCulture!);
            pubSubService.Publish(PubSubMessages.CULTURE_CHANGED, SelectedCulture);
        }

        NavigationManager.NavigateTo(NavigationManager.GetWithoutQueryString("culture"), forceLoad: true, replace: true);
    }

    private async Task ToggleTheme()
    {
        await bitDeviceCoordinator.ApplyTheme(await bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
