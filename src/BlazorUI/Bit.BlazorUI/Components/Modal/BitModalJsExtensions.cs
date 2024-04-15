namespace Bit.BlazorUI;

internal static class BitModalJsExtensions
{
    internal static async Task BitModalSetupDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        await js.InvokeVoidAsync("BitBlazorUI.Modal.setupDragDrop", id, dragElementSelector);
    }

    internal static async Task BitModalRemoveDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        await js.InvokeVoidAsync("BitBlazorUI.Modal.removeDragDrop", id, dragElementSelector);
    }
}
