namespace Bit.BlazorUI;

internal static class BitThemeJsExtensions
{
    internal static ValueTask<string> GetCurrentTheme(this IJSRuntime js)
    {
        return js.InvokeAsync<string>("BitTheme.getCurrentTheme");
    }

    internal static ValueTask<string> ToggleThemeDarkLight(this IJSRuntime js)
    {
        return js.InvokeAsync<string>("BitTheme.toggleThemeDarkLight");
    }

    internal static ValueTask SetTheme(this IJSRuntime js, string themeName)
    {
        return js.InvokeVoidAsync("BitTheme.setTheme", themeName);
    }

    internal static ValueTask ApplyBitTheme(this IJSRuntime js, Dictionary<string, string> theme, ElementReference? element)
    {
        return js.InvokeVoidAsync("BitTheme.applyBitTheme", theme, element);
    }
}
