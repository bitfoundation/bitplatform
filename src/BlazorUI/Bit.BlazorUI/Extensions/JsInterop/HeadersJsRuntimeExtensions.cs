namespace Bit.BlazorUI;

internal static class HeadersJsRuntimeExtensions
{
    internal static ValueTask BitHeadersSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitHeader> obj, int revealOffset, int elevateOffset, bool reveal, bool elevate, string? scrollTarget, bool scrollPadding, bool publishHeight)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Headers.setup", id, obj, revealOffset, elevateOffset, reveal, elevate, scrollTarget, scrollPadding, publishHeight);
    }

    internal static ValueTask BitHeadersDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Headers.dispose", id);
    }
}
