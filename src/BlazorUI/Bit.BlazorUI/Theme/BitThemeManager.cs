
namespace Bit.BlazorUI;

public static class BitThemeManager
{
    private static bool _isInitialized;
    private static IJSRuntime _js = default!;
    public static void Init(IJSRuntime js)
    {
        if(_isInitialized) return;

        _js = js;
        _isInitialized = true;
    }

    public static async Task ApplyBitTheme(BitTheme bitTheme, ElementReference? element = null)
    {
        CheckJsRuntime();

        if (bitTheme is null) return;

        await _js.ApplyBitTheme(BitThemeMapper.MapToCssVariables(bitTheme), element);
    }

    public static async Task SetTheme(string themeName)
    {
        CheckJsRuntime();

        await _js.SetTheme(themeName);
    }

    public static async Task<string> ToggleDarkLight()
    {
        CheckJsRuntime();

        return await _js.ToggleThemeDarkLight();
    }

    public static async Task<string> GetCurrentTheme()
    {
        CheckJsRuntime();

        return await _js.GetCurrentTheme();
    }

    private static void CheckJsRuntime()
    {
        if (_js is null)
            throw new InvalidOperationException("BitThemeManager is not initialized!");
    }
}
