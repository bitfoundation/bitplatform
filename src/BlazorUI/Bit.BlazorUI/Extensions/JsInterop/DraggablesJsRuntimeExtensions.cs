namespace Bit.BlazorUI;

internal static class DraggablesJsRuntimeExtensions
{
    internal static ValueTask BitDraggablesEnableDrag(this IJSRuntime jsRuntime, string id, string? selector = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Draggables.enableDrag", id, selector);
    }

    internal static ValueTask BitDraggablesDisableDrag(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Draggables.disableDrag", id);
    }
}
