namespace Bit.BlazorUI;

internal static class BitProModalJsRuntimeExtensions
{
    internal static ValueTask BitProModalSetupDragDrop(this IJSRuntime js, string containerSelector, string dragElementSelector)
    {
        return js.InvokeVoidAsync("BitBlazorUI.ProModal.setupDragDrop", containerSelector, dragElementSelector);
    }

    internal static ValueTask BitProModalRemoveDragDrop(this IJSRuntime js, string containerSelector, string dragElementSelector)
    {
        return js.InvokeVoidAsync("BitBlazorUI.ProModal.removeDragDrop", containerSelector, dragElementSelector);
    }

    internal static ValueTask<float> BitProModalToggleOverflow(this IJSRuntime js, string scrollerSelector, bool isHidden)
    {
        return js.InvokeAsync<float>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden);
    }
}
