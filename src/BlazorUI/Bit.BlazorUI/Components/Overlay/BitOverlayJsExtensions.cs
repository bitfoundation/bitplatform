
namespace Bit.BlazorUI;

internal static class BitOverlayJsExtensions
{
    internal static async Task<int> ToggleOverlayScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isVisible)
    {
        return await jsRuntime.InvokeAsync<int>("BitOverlay.toggleScroll", scrollerSelector, isVisible);
    }
}
