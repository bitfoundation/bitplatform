namespace Bit.BlazorUI;

internal static class DropdownsJsRuntimeExtensions
{
    internal static ValueTask BitDropdownsSetup(this IJSRuntime jsRuntime, string id, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Dropdowns.setup", id, calloutId);
    }

    internal static ValueTask BitDropdownsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Dropdowns.dispose", id);
    }

    internal static ValueTask BitDropdownsFocusItem(this IJSRuntime jsRuntime, string calloutId, string mode, string? character, bool virtualize, int selectedIndex, int itemSize)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Dropdowns.focusItem", calloutId, mode, character, virtualize, selectedIndex, itemSize);
    }
}
