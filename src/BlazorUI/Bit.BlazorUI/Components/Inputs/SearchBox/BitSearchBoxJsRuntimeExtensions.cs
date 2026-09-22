namespace Bit.BlazorUI;

internal static class BitSearchBoxJsRuntimeExtensions
{
    internal static ValueTask BitSearchBoxSetupInput(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.setupInput", input);
    }

    internal static ValueTask BitSearchBoxMoveCursorToEnd(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.moveCursorToEnd", input);
    }

    internal static ValueTask BitSearchBoxFillAndSelect(this IJSRuntime jsRuntime, ElementReference input, string value, int selectionStart)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.fillAndSelect", input, value, selectionStart);
    }

    internal static ValueTask BitSearchBoxScrollItemIntoView(this IJSRuntime jsRuntime, string containerId, string itemId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.scrollItemIntoView", containerId, itemId);
    }

    internal static ValueTask BitSearchBoxRegisterShortcut(this IJSRuntime jsRuntime, string inputId, string shortcut)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.registerShortcut", inputId, shortcut);
    }

    internal static ValueTask BitSearchBoxUnregisterShortcut(this IJSRuntime jsRuntime, string inputId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.unregisterShortcut", inputId);
    }
}
