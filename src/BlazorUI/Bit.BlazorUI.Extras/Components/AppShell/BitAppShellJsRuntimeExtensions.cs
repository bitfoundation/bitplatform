namespace Bit.BlazorUI;

internal static class BitAppShellJsRuntimeExtensions
{
    internal static ValueTask BitAppShellInitScroll(this IJSRuntime jsRuntime, ElementReference container, string url, bool historyOnly)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.initScroll", container, url, historyOnly);
    }

    internal static ValueTask BitAppShellLocationChangedScroll(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.locationChangedScroll", container);
    }

    internal static ValueTask BitAppShellAfterRenderScroll(this IJSRuntime jsRuntime, ElementReference container, string url, bool historyOnly)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.afterRenderScroll", container, url, historyOnly);
    }

    internal static ValueTask BitAppShellUpdateScroll(this IJSRuntime jsRuntime, ElementReference container, string url, bool historyOnly)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.updateScroll", container, url, historyOnly);
    }

    internal static ValueTask BitAppShellDisposeScroll(this IJSRuntime jsRuntime, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.disposeScroll", container);
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

    internal static ValueTask BitAppShellSetupScrollState(this IJSRuntime jsRuntime, string id, ElementReference root, ElementReference container)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.setupScrollState", id, root, container);
    }

    internal static ValueTask BitAppShellDisposeScrollState(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.disposeScrollState", id);
    }

    internal static ValueTask BitAppShellHoldScrollState(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.holdScrollState", id);
    }

    internal static ValueTask BitAppShellRegister(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.registerShell");
    }

    internal static ValueTask BitAppShellUnregister(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.unregisterShell");
    }
}
