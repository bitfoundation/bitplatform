namespace Bit.BlazorUI;

public class BitThemeManager
{
    private IJSRuntime _js = default!;

    public BitThemeManager(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask ApplyBitThemeAsync(BitTheme bitTheme, ElementReference? element = null)
    {
        if (bitTheme is null) return;

        await _js.ApplyBitTheme(BitThemeMapper.MapToCssVariables(bitTheme), element);
    }

    public async ValueTask SetThemeAsync(string themeName)
    {
        await _js.SetTheme(themeName);
    }

    public async ValueTask<string> ToggleDarkLightAsync()
    {
        return await _js.ToggleThemeDarkLight();
    }

    public async ValueTask<string> GetCurrentThemeAsync()
    {
        return await _js.GetCurrentTheme();
    }
}
