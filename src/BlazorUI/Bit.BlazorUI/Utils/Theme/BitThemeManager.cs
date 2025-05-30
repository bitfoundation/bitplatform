namespace Bit.BlazorUI;

public class BitThemeManager
{
    private IJSRuntime _js = default!;

    public BitThemeManager(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<string> GetCurrentThemeAsync()
    {
        return await _js.BitThemeGetCurrentTheme();
    }

    public async ValueTask<string> SetThemeAsync(string themeName)
    {
        return await _js.BitThemeSetTheme(themeName);
    }

    public async ValueTask<string> ToggleDarkLightAsync()
    {
        return await _js.BitThemeToggleThemeDarkLight();
    }

    public async ValueTask ApplyBitThemeAsync(BitTheme bitTheme, ElementReference? element = null)
    {
        if (bitTheme is null) return;

        await _js.BitThemeApplyBitTheme(BitThemeMapper.MapToCssVariables(bitTheme), element);
    }

    public async ValueTask<bool> IsSystemInDarkMode()
    {
        return await _js.BitThemeIsSystemDark();
    }

    public async ValueTask<string> GetCurrentPersistedThemeAsync()
    {
        return await _js.BitThemeGetCurrentPersistedTheme();
    }
}
