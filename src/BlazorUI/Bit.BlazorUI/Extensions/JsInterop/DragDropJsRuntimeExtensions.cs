namespace Bit.BlazorUI;

internal static class DragDropJsRuntimeExtensions
{
    internal static ValueTask BitDragDropSetup(this IJSRuntime jsRuntime, string key, string containerSelector, string dragElementSelector)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.DragDrop.setup", key, containerSelector, dragElementSelector);
    }


    internal static ValueTask BitDragDropRemove(this IJSRuntime jsRuntime, string key, string dragElementSelector)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.DragDrop.remove", key, dragElementSelector);
    }
}
