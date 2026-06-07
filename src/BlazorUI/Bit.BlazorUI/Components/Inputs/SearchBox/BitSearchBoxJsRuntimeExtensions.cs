namespace Bit.BlazorUI;

internal static class BitSearchBoxJsRuntimeExtensions
{
    internal static ValueTask BitSearchBoxMoveCursorToEnd(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.SearchBox.moveCursorToEnd", input);
    }
}
