namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class ThemeService
{
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;


    public async Task<AppThemeType> GetCurrentTheme()
    {
        return ToAppTheme(await bitThemeManager.GetCurrentThemeAsync());
    }

    public async Task<AppThemeType> ToggleTheme()
    {
        var theme = ToAppTheme(await bitThemeManager.ToggleDarkLightAsync());

        await bitDeviceCoordinator.ApplyTheme(theme is AppThemeType.Dark);

        pubSubService.Publish(ClientAppMessages.THEME_CHANGED, theme);

        return theme;
    }

    private static AppThemeType ToAppTheme(string? themeName)
    {
        return themeName?.EndsWith("dark", StringComparison.OrdinalIgnoreCase) is true
            ? AppThemeType.Dark
            : AppThemeType.Light;
    }
}
