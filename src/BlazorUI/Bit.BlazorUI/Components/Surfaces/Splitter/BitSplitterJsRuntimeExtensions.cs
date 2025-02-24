using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitSplitterJsRuntimeExtensions
{
    internal static ValueTask BitSplitterResetPaneDimensions(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Splitter.resetPaneDimensions", element);
    }

    internal static ValueTask<double> BitSplitterGetSplitterWidth(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvoke<double>("BitBlazorUI.Splitter.getSplitterWidth", element);
    }

    internal static ValueTask BitSplitterSetSplitterWidth(this IJSRuntime jsRuntime, ElementReference element, double value)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Splitter.setSplitterWidth", element, value);
    }

    internal static ValueTask<double> BitSplitterGetSplitterHeight(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvoke<double>("BitBlazorUI.Splitter.getSplitterHeight", element);
    }

    internal static ValueTask BitSplitterSetSplitterHeight(this IJSRuntime jsRuntime, ElementReference element, double value)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Splitter.setSplitterHeight", element, value);
    }

    internal static ValueTask BitSplitterHandleSplitterDragging(this IJSRuntime jsRuntime, TouchEventArgs e)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Splitter.handleSplitterDragging", e);
    }

    internal static ValueTask BitSplitterHandleSplitterDraggingEnd(this IJSRuntime jsRuntime)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Splitter.handleSplitterDraggingEnd");
    }
}
