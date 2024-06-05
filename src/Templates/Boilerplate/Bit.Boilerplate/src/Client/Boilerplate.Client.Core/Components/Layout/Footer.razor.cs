﻿//-:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Layout;

public partial class Footer
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IPubSubService pubSubService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;

    private BitDropdownItem<string>[] cultures = default!;

    protected override Task OnInitAsync()
    {
        cultures = CultureInfoManager.SupportedUICultures
                                      .Select(sc => new BitDropdownItem<string> { Value = sc.Name, Text = sc.DisplayName })
                                      .ToArray();

        SelectedCulture = CultureInfo.CurrentUICulture.Name;

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

        if (AppRenderMode.IsBlazorHybrid)
        {
            cultureInfoManager.SetCurrentCulture(SelectedCulture!);
            pubSubService.Publish(PubSubMessages.CULTURE_CHANGED, SelectedCulture);
        }

        NavigationManager.Refresh(forceReload: true);
    }

    private async Task ToggleTheme()
    {
        await bitDeviceCoordinator.ApplyTheme(await bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
