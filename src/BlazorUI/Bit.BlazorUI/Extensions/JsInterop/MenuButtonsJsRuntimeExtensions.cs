namespace Bit.BlazorUI;

internal static class MenuButtonsJsRuntimeExtensions
{
    internal static ValueTask BitMenuButtonsSetup(this IJSRuntime jsRuntime, string id, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MenuButtons.setup", id, calloutId);
    }

    internal static ValueTask BitMenuButtonsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MenuButtons.dispose", id);
    }

    // The keydown listener of a submenu, which is a menu of its own: it is registered under the menu
    // button that owns it, so disposing the component takes every menu of it away at once.
    internal static ValueTask BitMenuButtonsSetupSubmenu(this IJSRuntime jsRuntime, string id, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MenuButtons.setupSubmenu", id, calloutId);
    }

    internal static ValueTask BitMenuButtonsDisposeSubmenu(this IJSRuntime jsRuntime, string id, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MenuButtons.disposeSubmenu", id, calloutId);
    }

    // The culture is the one a "char" search is matched in (the name of the current culture of .NET, empty for the
    // invariant one); without one the script matches the way it always has.
    internal static ValueTask BitMenuButtonsFocusItem(this IJSRuntime jsRuntime, string calloutId, string mode, string? character, bool includeDisabled, bool fromCurrent, string? culture = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MenuButtons.focusItem", calloutId, mode, character, includeDisabled, fromCurrent, culture);
    }
}
