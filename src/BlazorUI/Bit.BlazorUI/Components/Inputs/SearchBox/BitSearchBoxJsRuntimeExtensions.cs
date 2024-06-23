namespace Bit.BlazorUI;

internal static class BitSearchBoxJsRuntimeExtensions
{
    internal static ValueTask BitSearchBoxMoveCursorToEnd(this IJSRuntime jsRuntime, ElementReference input)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.SearchBox.moveCursorToEnd", input);
    }
}
