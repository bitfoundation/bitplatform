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

    internal static ValueTask BitMenuButtonsFocusItem(this IJSRuntime jsRuntime, string calloutId, string mode, string? character)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.MenuButtons.focusItem", calloutId, mode, character);
    }
}
