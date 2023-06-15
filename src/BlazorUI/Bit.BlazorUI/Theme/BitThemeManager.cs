using Bit.BlazorUI.Theme;

namespace Bit.BlazorUI;

public static class BitThemeManager
{
    private static IJSRuntime _js = default!;
    public static void init(IJSRuntime js)
    {
        _js = js;
    }

    public static async Task ApplyBitTheme(BitTheme bitTheme, ElementReference? element = null)
    {
        CheckJsRuntime();

        await _js.ApplyBitTheme(BitThemeMapper.MapToCssVariables(bitTheme), element);
    }

    public static async Task ChangeTheme(string themeName)
    {
        CheckJsRuntime();

        await _js.ChangeTheme(themeName);
    }

    public static async Task ToggleDarkLight(bool isDark)
    {
        CheckJsRuntime();

        await _js.ChangeTheme(isDark ? "dark" : "light");
    }

    private static void CheckJsRuntime()
    {
        if (_js is null)
            throw new InvalidOperationException("BitTheme is not initialized!");
    }
}
