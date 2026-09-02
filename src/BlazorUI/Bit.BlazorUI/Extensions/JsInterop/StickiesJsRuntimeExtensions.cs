namespace Bit.BlazorUI;

internal static class StickiesJsRuntimeExtensions
{
    internal static ValueTask BitStickiesSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitSticky> obj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Stickies.setup", id, obj);
    }

    internal static ValueTask BitStickiesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Stickies.dispose", id);
    }
}
