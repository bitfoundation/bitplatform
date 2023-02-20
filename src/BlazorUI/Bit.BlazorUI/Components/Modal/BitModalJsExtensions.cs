
namespace Bit.BlazorUI;

internal static class BitModalJsExtensions
{
    internal static async Task<int> ToggleModalScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isOpen)
    {
        return await jsRuntime.InvokeAsync<int>("BitModal.toggleScroll", scrollerSelector, isOpen);
    }
}
