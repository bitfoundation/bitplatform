namespace Bit.BlazorUI;

internal static class BitThemeJsExtensions
{
    internal static ValueTask<string> BitThemeGetCurrentTheme(this IJSRuntime js)
    {
        return js.Invoke<string>("BitTheme.get");
    }

    internal static ValueTask<string> BitThemeSetTheme(this IJSRuntime js, string themeName)
    {
        return js.Invoke<string>("BitTheme.set", themeName);
    }

    internal static ValueTask<string> BitThemeToggleThemeDarkLight(this IJSRuntime js)
    {
        return js.Invoke<string>("BitTheme.toggleDarkLight");
    }

    internal static ValueTask BitThemeApplyBitTheme(this IJSRuntime js, Dictionary<string, string> theme, ElementReference? element)
    {
        return js.InvokeVoid("BitTheme.applyBitTheme", theme, element);
    }

    internal static ValueTask<string> BitThemeGetCurrentPersistedTheme(this IJSRuntime js)
    {
        return js.Invoke<string>("BitTheme.getPersisted");
    }
}
