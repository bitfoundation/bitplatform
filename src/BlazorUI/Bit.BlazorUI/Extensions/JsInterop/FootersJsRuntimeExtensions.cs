namespace Bit.BlazorUI;

internal static class FootersJsRuntimeExtensions
{
    internal static ValueTask BitFootersSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitFooter> obj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Footers.setup", id, obj);
    }

    internal static ValueTask BitFootersDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Footers.dispose", id);
    }
}
