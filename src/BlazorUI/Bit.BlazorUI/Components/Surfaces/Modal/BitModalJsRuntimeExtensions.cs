namespace Bit.BlazorUI;

internal static class BitModalJsRuntimeExtensions
{
    internal static ValueTask BitModalSetupDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        return js.InvokeVoid("BitBlazorUI.Modal.setupDragDrop", id, dragElementSelector);
    }

    internal static ValueTask BitModalRemoveDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        return js.InvokeVoid("BitBlazorUI.Modal.removeDragDrop", id, dragElementSelector);
    }
}
