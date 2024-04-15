namespace Bit.BlazorUI;

internal static class BitOverlayJsExtensions
{
    internal static async Task<int> BitOverlayToggleScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isVisible)
    {
        return await jsRuntime.InvokeAsync<int>("BitBlazorUI.Overlay.toggleScroll", scrollerSelector, isVisible);
    }
}
