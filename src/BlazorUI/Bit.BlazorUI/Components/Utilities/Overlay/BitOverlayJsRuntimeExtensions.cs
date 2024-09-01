namespace Bit.BlazorUI;

internal static class BitOverlayJsRuntimeExtensions
{
    internal static ValueTask<int> BitOverlayToggleScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isOpen)
    {
        return jsRuntime.InvokeAsync<int>("BitBlazorUI.Overlay.toggleScroll", scrollerSelector, isOpen);
    }
}
