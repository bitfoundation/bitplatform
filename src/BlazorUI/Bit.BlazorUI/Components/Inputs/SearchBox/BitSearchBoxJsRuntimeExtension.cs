namespace Bit.BlazorUI;

internal static class BitSearchBoxJsRuntimeExtension
{
    internal static async Task BitSearchBoxMoveCursorToEnd(this IJSRuntime jsRuntime, ElementReference input)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.SearchBox.moveCursorToEnd", input);
    }
}
