
namespace Bit.BlazorUI;

internal static class BitOverlayJsExtensions
{
    internal static async void ToggleScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isVisible)
    {
        await jsRuntime.InvokeVoidAsync("BitOverlay.toggleScroll", scrollerSelector, isVisible);
    }
}
