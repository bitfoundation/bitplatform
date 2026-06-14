namespace Bit.BlazorUI;

internal static class UtilsJsRuntimeExtensions
{
    // FastInvoke returns null when the runtime can't service interop or a JSON/JS interop error is
    // swallowed on the in-process (WASM) path. Nullable distinguishes that from a legitimate 0 width.
    internal static ValueTask<decimal?> BitUtilsGetBodyWidth(this IJSRuntime jsRuntime)
    {
        return jsRuntime.FastInvoke<decimal?>("BitBlazorUI.Utils.getBodyWidth");
    }


    internal static ValueTask BitUtilsSetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object? value)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Utils.setProperty", element, property, value);
    }


    internal static ValueTask<string?> BitUtilsGetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return jsRuntime.FastInvoke<string?>("BitBlazorUI.Utils.getProperty", element, property);
    }


    internal static ValueTask<BoundingClientRect?> BitUtilsGetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvoke<BoundingClientRect?>("BitBlazorUI.Utils.getBoundingClientRect", element);
    }


    internal static ValueTask BitUtilsScrollElementIntoView(this IJSRuntime jsRuntime, string targetElementId)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Utils.scrollElementIntoView", targetElementId);
    }


    internal static ValueTask BitUtilsSelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Utils.selectText", element);
    }


    internal static ValueTask BitUtilsSetStyle(this IJSRuntime jsRuntime, ElementReference element, string key, string value)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Utils.setStyle", element, key, value);
    }


    internal static ValueTask<float?> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, string scrollerSelector, bool isHidden)
    {
        return jsRuntime.FastInvoke<float?>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden);
    }

    internal static ValueTask<float?> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, ElementReference scrollerElement, bool isHidden)
    {
        return jsRuntime.FastInvoke<float?>("BitBlazorUI.Utils.toggleOverflow", scrollerElement, isHidden);
    }
}
