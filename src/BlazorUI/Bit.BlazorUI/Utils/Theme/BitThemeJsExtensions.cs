namespace Bit.BlazorUI;

internal static class BitThemeJsExtensions
{
    internal static ValueTask<string> GetCurrentTheme(this IJSRuntime js)
    {
        return js.Invoke<string>("BitTheme.get");
    }

    internal static ValueTask<string> ToggleThemeDarkLight(this IJSRuntime js)
    {
        return js.Invoke<string>("BitTheme.toggleDarkLight");
    }

    internal static ValueTask SetTheme(this IJSRuntime js, string themeName)
    {
        return js.InvokeVoid("BitTheme.set", themeName);
    }

    internal static ValueTask ApplyBitTheme(this IJSRuntime js, Dictionary<string, string> theme, ElementReference? element)
    {
        return js.InvokeVoid("BitTheme.applyBitTheme", theme, element);
    }
}
