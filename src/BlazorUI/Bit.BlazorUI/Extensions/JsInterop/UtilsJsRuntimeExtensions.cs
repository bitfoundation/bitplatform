using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class UtilsJsRuntimeExtensions
{
    internal static ValueTask<decimal> BitUtilsGetBodyWidth(this IJSRuntime jsRuntime)
    {
        return jsRuntime.FastInvoke<decimal>("BitBlazorUI.Utils.getBodyWidth");
    }


    internal static ValueTask BitUtilsSetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object? value)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Utils.setProperty", element, property, value);
    }


    internal static ValueTask<string> BitUtilsGetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return jsRuntime.FastInvoke<string>("BitBlazorUI.Utils.getProperty", element, property);
    }


    internal static ValueTask<BoundingClientRect> BitUtilsGetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvoke<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", element);
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


    internal static ValueTask<int> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, string scrollerSelector, bool isHidden)
    {
        return jsRuntime.FastInvoke<int>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden);
    }
}
