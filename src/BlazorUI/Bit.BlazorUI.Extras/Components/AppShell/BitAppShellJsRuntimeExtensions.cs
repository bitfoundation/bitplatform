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

    internal static ValueTask BitAppShellClearScrolls(this IJSRuntime jsRuntime, string? url = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.clearScrolls", url);
    }

    internal static ValueTask BitAppShellSetupKeyboard<T>(this IJSRuntime jsRuntime,
                                                          string id,
                                                          ElementReference element,
                                                          DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.setupKeyboard", id, element, dotnetObj);
    }

    internal static ValueTask BitAppShellDisposeKeyboard(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.disposeKeyboard", id);
    }
}
