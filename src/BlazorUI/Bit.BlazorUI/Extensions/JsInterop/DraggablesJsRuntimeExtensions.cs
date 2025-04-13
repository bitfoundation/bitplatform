using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class DraggablesJsRuntimeExtensions
{
    internal static ValueTask BitDraggablesSetup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(
        this IJSRuntime jsRuntime,
        string id,
        DotNetObjectReference<T>? dotnetObj,
        string? selector = null) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Draggables.setup", id, dotnetObj, selector);
    }

    internal static ValueTask BitDraggablesDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Draggables.dispose", id);
    }
}
