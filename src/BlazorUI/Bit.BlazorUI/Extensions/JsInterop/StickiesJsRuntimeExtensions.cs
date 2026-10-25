namespace Bit.BlazorUI;

internal static class StickiesJsRuntimeExtensions
{
    internal static ValueTask BitStickiesSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitSticky> obj, bool report, bool scrollPadding)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Stickies.setup", id, obj, report, scrollPadding);
    }

    internal static ValueTask BitStickiesRefresh(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Stickies.refresh", id);
    }

    internal static ValueTask BitStickiesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Stickies.dispose", id);
    }
}
