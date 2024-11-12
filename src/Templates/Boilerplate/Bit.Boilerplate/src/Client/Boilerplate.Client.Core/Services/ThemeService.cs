namespace Boilerplate.Client.Core.Services;

public partial class ThemeService
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;


    public async Task<AppThemeType> GetCurrentTheme()
    {
        var theme = await bitThemeManager.GetCurrentThemeAsync();
        return theme == "dark" ? AppThemeType.Dark : AppThemeType.Light;
    }

    public async Task<AppThemeType> ToggleTheme()
    {
        var newThemeName = await bitThemeManager.ToggleDarkLightAsync();
        
        var isDark = newThemeName == "dark";
        await bitDeviceCoordinator.ApplyTheme(isDark);

        var theme = isDark ? AppThemeType.Dark : AppThemeType.Light;
        pubSubService.Publish(ClientPubSubMessages.THEME_CHANGED, theme);

        await cookie.Set(new()
        {
            MaxAge = 30 * 24 * 3600,
            Name = "bit.Theme",
            Secure = AppEnvironment.IsDev() is false,
            Value = Uri.EscapeDataString(newThemeName),
        });

        return theme;
    }
}
