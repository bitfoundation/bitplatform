namespace Bit.BlazorUI;

internal static class BitThemeJsExtensions
{
    internal static ValueTask<string?> BitThemeGetCurrentTheme(this IJSRuntime js)
    {
        return js.Invoke<string?>("BitBlazorUI.Theme.get");
    }

    internal static ValueTask<string?> BitThemeSetTheme(this IJSRuntime js, string themeName)
    {
        return js.Invoke<string?>("BitBlazorUI.Theme.set", themeName);
    }

    internal static ValueTask<string?> BitThemeToggleThemeDarkLight(this IJSRuntime js)
    {
        return js.Invoke<string?>("BitBlazorUI.Theme.toggleDarkLight");
    }

    internal static ValueTask BitThemeApplyBitTheme(this IJSRuntime js, IReadOnlyDictionary<string, string> theme, ElementReference? element)
    {
        return js.InvokeVoid("BitBlazorUI.Theme.applyTheme", theme, element);
    }

    internal static ValueTask<bool> BitThemeIsSystemDark(this IJSRuntime js)
    {
        return js.Invoke<bool>("BitBlazorUI.Theme.isSystemDark");
    }

    internal static ValueTask<string?> BitThemeGetCurrentPersistedTheme(this IJSRuntime js)
    {
        return js.Invoke<string?>("BitBlazorUI.Theme.getPersisted");
    }

    internal static ValueTask BitThemeClearAppliedBitTheme(this IJSRuntime js, ElementReference? element)
    {
        return js.InvokeVoid("BitBlazorUI.Theme.clearAppliedTheme", element);
    }

    internal static ValueTask BitThemeRegisterDotNetNotifier(this IJSRuntime js, DotNetObjectReference<BitThemeJsNotifierReceiver> dotNetRef)
    {
        return js.InvokeVoid("BitBlazorUI.Theme.registerDotNetNotifier", dotNetRef);
    }

    internal static ValueTask BitThemeUnregisterDotNetNotifier(this IJSRuntime js)
    {
        return js.InvokeVoid("BitBlazorUI.Theme.unregisterDotNetNotifier");
    }
}
