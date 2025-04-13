using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class DraggablesJsRuntimeExtensions
{
    internal static ValueTask BitDraggablesEnable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        this IJSRuntime jsRuntime,
        string id,
        DotNetObjectReference<T>? dotnetObj = null,
        string? selector = null) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Draggables.enable", id, dotnetObj, selector);
    }

    internal static ValueTask BitDraggablesDisable(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Draggables.disable", id);
    }
}
