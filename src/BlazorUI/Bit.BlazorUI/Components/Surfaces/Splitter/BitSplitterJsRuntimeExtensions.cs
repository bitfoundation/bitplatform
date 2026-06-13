namespace Bit.BlazorUI;

internal static class BitSplitterJsRuntimeExtensions
{
    internal static ValueTask BitSplitterResetPaneDimensions(this IJSRuntime js, ElementReference element)
    {
        return js.FastInvokeVoid("BitBlazorUI.Splitter.resetPaneDimensions", element);
    }

    // FastInvoke returns null when the runtime can't service interop or a JSON/JS interop error is
    // swallowed on the in-process (WASM) path. Nullable distinguishes that from a legitimate 0 width.
    internal static ValueTask<double?> BitSplitterGetSplitterWidth(this IJSRuntime js, ElementReference element)
    {
        return js.FastInvoke<double?>("BitBlazorUI.Splitter.getSplitterWidth", element);
    }

    internal static ValueTask BitSplitterSetSplitterWidth(this IJSRuntime js, ElementReference element, double value)
    {
        return js.FastInvokeVoid("BitBlazorUI.Splitter.setSplitterWidth", element, value);
    }

    internal static ValueTask<double?> BitSplitterGetSplitterHeight(this IJSRuntime js, ElementReference element)
    {
        return js.FastInvoke<double?>("BitBlazorUI.Splitter.getSplitterHeight", element);
    }

    internal static ValueTask BitSplitterSetSplitterHeight(this IJSRuntime js, ElementReference element, double value)
    {
        return js.FastInvokeVoid("BitBlazorUI.Splitter.setSplitterHeight", element, value);
    }

    internal static ValueTask BitSplitterHandleSplitterDragging(this IJSRuntime js, TouchEventArgs e)
    {
        return js.FastInvokeVoid("BitBlazorUI.Splitter.handleSplitterDragging", e);
    }

    internal static ValueTask BitSplitterHandleSplitterDraggingEnd(this IJSRuntime js)
    {
        return js.FastInvokeVoid("BitBlazorUI.Splitter.handleSplitterDraggingEnd");
    }
}
