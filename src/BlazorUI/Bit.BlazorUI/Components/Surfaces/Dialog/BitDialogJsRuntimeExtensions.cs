namespace Bit.BlazorUI;

internal static class BitDialogJsRuntimeExtensions
{
    internal static ValueTask BitDialogSetupDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        return js.InvokeVoid("BitBlazorUI.Dialog.setupDragDrop", id, dragElementSelector);
    }

    internal static ValueTask BitDialogRemoveDragDrop(this IJSRuntime js, string id, string dragElementSelector)
    {
        return js.InvokeVoid("BitBlazorUI.Dialog.removeDragDrop", id, dragElementSelector);
    }
}
