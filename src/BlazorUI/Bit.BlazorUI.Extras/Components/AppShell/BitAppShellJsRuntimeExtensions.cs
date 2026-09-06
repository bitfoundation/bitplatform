namespace Bit.BlazorUI;

internal static class BitAppShellJsRuntimeExtensions
{
    internal static ValueTask BitAppShellInitScroll(this IJSRuntime jsRuntime, ElementReference container, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.initScroll", container, url);
    }

    internal static ValueTask BitAppShellLocationChangedScroll(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.locationChangedScroll");
    }

    internal static ValueTask BitAppShellAfterRenderScroll(this IJSRuntime jsRuntime, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.afterRenderScroll", url);
    }

    internal static ValueTask BitAppShellDisposeScroll(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.disposeScroll");
    }

    internal static ValueTask BitAppShellClearScrolls(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.clearScrolls");
    }

    internal static ValueTask BitAppShellSetupKeyboard(this IJSRuntime jsRuntime, string id, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.setupKeyboard", id, element);
    }

    internal static ValueTask BitAppShellDisposeKeyboard(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.disposeKeyboard", id);
    }
}
