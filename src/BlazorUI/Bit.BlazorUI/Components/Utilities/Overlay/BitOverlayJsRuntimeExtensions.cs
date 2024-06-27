namespace Bit.BlazorUI;

internal static class BitOverlayJsRuntimeExtensions
{
    internal static ValueTask<int> BitOverlayToggleScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isVisible)
    {
        return jsRuntime.InvokeAsync<int>("BitBlazorUI.Overlay.toggleScroll", scrollerSelector, isVisible);
    }
}
