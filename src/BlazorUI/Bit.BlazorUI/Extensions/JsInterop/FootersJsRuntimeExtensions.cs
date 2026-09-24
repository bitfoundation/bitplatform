namespace Bit.BlazorUI;

internal static class FootersJsRuntimeExtensions
{
    internal static ValueTask BitFootersSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitFooter> obj, int revealOffset, bool reveal, string? scrollTarget, bool scrollPadding)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Footers.setup", id, obj, revealOffset, reveal, scrollTarget, scrollPadding);
    }

    internal static ValueTask BitFootersDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Footers.dispose", id);
    }
}
