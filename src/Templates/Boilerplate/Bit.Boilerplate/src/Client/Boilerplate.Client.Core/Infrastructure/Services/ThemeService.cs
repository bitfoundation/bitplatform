namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class ThemeService
{
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;

    /// <summary>
    /// Read by the native heads at launch, before their WebView exists (See MauiProgram and Windows' Program).
    /// </summary>
    public const string THEME_STORAGE_KEY = "Theme";

    public async Task<AppThemeType> GetCurrentTheme()
    {
        return ToAppTheme(await bitThemeManager.GetCurrentThemeAsync());
    }

    public async Task<AppThemeType> ToggleTheme()
    {
        var theme = ToAppTheme(await bitThemeManager.ToggleDarkLightAsync());

        if (AppPlatform.IsBlazorHybrid)
        {
            // bit BlazorUI's own copy lives in the WebView, which isn't up yet when the native chrome is painted.
            await storageService.SetItem(THEME_STORAGE_KEY, theme.ToString(), persistent: true);
        }

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
