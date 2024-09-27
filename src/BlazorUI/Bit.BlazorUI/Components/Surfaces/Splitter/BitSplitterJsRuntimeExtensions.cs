namespace Bit.BlazorUI;

internal static class BitSplitterJsRuntimeExtensions
{
    internal static ValueTask BitSplitterResetPaneDimensions(this IJSRuntime js, ElementReference element)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.resetPaneDimensions", element);
    }

    internal static ValueTask<double> BitSplitterGetSplitterWidth(this IJSRuntime js, ElementReference element)
    {
        return js.Invoke<double>("BitBlazorUI.Splitter.getSplitterWidth", element);
    }

    internal static ValueTask BitSplitterSetSplitterWidth(this IJSRuntime js, ElementReference element, double value)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.setSplitterWidth", element, value);
    }

    internal static ValueTask<double> BitSplitterGetSplitterHeight(this IJSRuntime js, ElementReference element)
    {
        return js.Invoke<double>("BitBlazorUI.Splitter.getSplitterHeight", element);
    }

    internal static ValueTask BitSplitterSetSplitterHeight(this IJSRuntime js, ElementReference element, double value)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.setSplitterHeight", element, value);
    }

    internal static ValueTask BitSplitterHandleSplitterDragging(this IJSRuntime js, TouchEventArgs e)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.handleSplitterDragging", e);
    }

    internal static ValueTask BitSplitterHandleSplitterDraggingEnd(this IJSRuntime js)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.handleSplitterDraggingEnd");
    }
}
