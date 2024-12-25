namespace Bit.BlazorUI;

internal static class DragDropsJsRuntimeExtensions
{
    internal static ValueTask DragDropsSetup(this IJSRuntime js, string id, string dragElementSelector)
    {
        return js.InvokeVoid("BitBlazorUI.DragDrops.setup", id, dragElementSelector);
    }

    internal static ValueTask DragDropsRemove(this IJSRuntime js, string id, string dragElementSelector)
    {
        return js.InvokeVoid("BitBlazorUI.DragDrops.remove", id, dragElementSelector);
    }
}
