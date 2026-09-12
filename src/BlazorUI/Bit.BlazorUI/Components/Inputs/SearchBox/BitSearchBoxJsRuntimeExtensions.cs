namespace Bit.BlazorUI;

internal static class BitSearchBoxJsRuntimeExtensions
{
    internal static ValueTask BitSearchBoxSetupInput(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.setupInput", input);
    }

    internal static ValueTask BitSearchBoxMoveCursorToEnd(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.SearchBox.moveCursorToEnd", input);
    }

    internal static ValueTask BitSearchBoxScrollItemIntoView(this IJSRuntime jsRuntime, string containerId, string itemId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SearchBox.scrollItemIntoView", containerId, itemId);
    }
}
