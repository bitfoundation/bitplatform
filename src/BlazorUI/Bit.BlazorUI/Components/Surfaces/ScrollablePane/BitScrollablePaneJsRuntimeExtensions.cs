namespace Bit.BlazorUI;

internal static class BitScrollablePaneJsRuntimeExtensions
{
    internal static ValueTask BitScrollablePaneScrollToEnd(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.ScrollablePane.scrollToEnd", element);
    }
}
