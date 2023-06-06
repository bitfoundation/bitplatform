namespace Bit.BlazorUI;

public static class BitThemeManager
{
    private static IJSRuntime _js = default!;
    public static void init(IJSRuntime js)
    {
        _js = js;
    }

    public static async Task ChangeTheme(string themeName)
    {
        CheckJsRuntime();

        await _js.InvokeVoidAsync("Bit.changeTheme", themeName);
    }

    public static async Task ToggleDarkLight(bool isDark)
    {
        CheckJsRuntime();

        await _js.InvokeVoidAsync("Bit.changeTheme", isDark ? "dark" : "light");
    }

    private static void CheckJsRuntime()
    {
        if (_js is null)
            throw new InvalidOperationException("BitTheme is not initialized!");
    }
}
