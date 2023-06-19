namespace Bit.BlazorUI;
internal static class BitThemeJsExtensions
{
    internal static async Task<string> GetCurrentTheme(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<string>("BitTheme.getCurrentTheme");
    }

    internal static async Task<string> ToggleThemeDarkLight(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<string>("BitTheme.toggleThemeDarkLight");
    }

    internal static async Task SetTheme(this IJSRuntime jsRuntime, string themeName)
    {
        await jsRuntime.InvokeVoidAsync("BitTheme.setTheme", themeName);
    }

    internal static async Task ApplyBitTheme(this IJSRuntime jsRuntime, Dictionary<string, string> theme, ElementReference? element)
    {
        await jsRuntime.InvokeVoidAsync("BitTheme.applyBitTheme", theme, element);
    }
}
