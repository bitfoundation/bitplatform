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

    // The search box is reached from C# by the id of the callout it lives in rather than by an
    // ElementReference: the reference is only assigned while the box is rendered, and the caller is the
    // trigger, which is handling a key pressed before the box existed on the page at all.
    internal static ValueTask BitDropdownsFocusSearchBox(this IJSRuntime jsRuntime, string calloutId, int caret)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Dropdowns.focusSearchBox", calloutId, caret);
    }

    internal static ValueTask BitDropdownsFocusItem(this IJSRuntime jsRuntime, string calloutId, BitDropdownFocusMode mode, string? character, bool virtualize, int selectedIndex, int itemSize, bool noWrap)
    {
        // Mapped by hand rather than by name: these are the exact strings Dropdowns.ts branches on, so
        // renaming a member of the enum must not silently change what is sent over the wire.
        var modeValue = mode switch
        {
            BitDropdownFocusMode.Selected => "selected",
            BitDropdownFocusMode.First => "first",
            BitDropdownFocusMode.Last => "last",
            BitDropdownFocusMode.Next => "next",
            BitDropdownFocusMode.Prev => "prev",
            BitDropdownFocusMode.NextPage => "nextPage",
            BitDropdownFocusMode.PrevPage => "prevPage",
            BitDropdownFocusMode.Char => "char",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return jsRuntime.InvokeVoid("BitBlazorUI.Dropdowns.focusItem", calloutId, modeValue, character, virtualize, selectedIndex, itemSize, noWrap);
    }
}
