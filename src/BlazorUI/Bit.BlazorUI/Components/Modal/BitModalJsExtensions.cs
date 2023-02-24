namespace Bit.BlazorUI;

internal static class BitModalJsExtensions
{
    internal static async Task<int> ToggleModalScroll(this IJSRuntime jsRuntime, string scrollerSelector, bool isOpen)
    {
        return await jsRuntime.InvokeAsync<int>("BitModal.toggleScroll", scrollerSelector, isOpen);
    }

    internal static async Task SetupDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        await js.InvokeVoidAsync("BitModal.setupDragDrop", id, dragElementSelector);
    }

    internal static async Task RemoveDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        await js.InvokeVoidAsync("BitModal.removeDragDrop", id, dragElementSelector);
    }
}
