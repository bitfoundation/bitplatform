
namespace Bit.BlazorUI;

internal static class BitModalJsExtensions
{
    internal static async void ToggleModalScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isOpen)
    {
        await jsRuntime.InvokeVoidAsync("BitModal.toggleScroll", scrollerSelector, isOpen);
    }
}
