namespace Bit.BlazorUI;

public class BitThemeManager
{
    private IJSRuntime _js = default!;

    public BitThemeManager(IJSRuntime js)
    {
        _js = js;
    }

    public async Task ApplyBitThemeAsync(BitTheme bitTheme, ElementReference? element = null)
    {
        if (bitTheme is null) return;

        await _js.ApplyBitTheme(BitThemeMapper.MapToCssVariables(bitTheme), element);
    }

    public async Task SetThemeAsync(string themeName)
    {
        await _js.SetTheme(themeName);
    }

    public async Task<string> ToggleDarkLightAsync()
    {
        return await _js.ToggleThemeDarkLight();
    }

    public async Task<string> GetCurrentThemeAsync()
    {
        return await _js.GetCurrentTheme();
    }
}
