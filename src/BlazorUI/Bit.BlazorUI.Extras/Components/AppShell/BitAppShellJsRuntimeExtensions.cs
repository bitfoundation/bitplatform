using System.ComponentModel;

namespace Bit.BlazorUI;

internal static class BitAppShellJsRuntimeExtensions
{
    internal static ValueTask BitAppShellInitScroll(this IJSRuntime jsRuntime, ElementReference container, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.initScroll", container, url);
    }

    internal static ValueTask BitAppShellLocationChangedScroll(this IJSRuntime jsRuntime, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.locationChangedScroll", url);
    }

    internal static ValueTask BitAppShellAfterRenderScroll(this IJSRuntime jsRuntime, string url)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.afterRenderScroll", url);
    }

    internal static ValueTask BitAppShellDisposeScroll(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AppShell.disposeScroll");
    }
}
