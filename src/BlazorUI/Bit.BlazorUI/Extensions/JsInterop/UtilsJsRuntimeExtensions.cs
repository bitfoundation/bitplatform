using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class UtilsJsRuntimeExtensions
{
    internal static ValueTask<decimal> BitUtilsGetBodyWidth(this IJSRuntime jsRuntime)
    {
        return jsRuntime.Invoke<decimal>("BitBlazorUI.Utils.getBodyWidth");
    }


    internal static ValueTask BitUtilsSetProperty(this IJSRuntime jsRuntime, ElementReference element, string property, object? value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setProperty", element, property, value);
    }


    internal static ValueTask<string> BitUtilsGetProperty(this IJSRuntime jsRuntime, ElementReference element, string property)
    {
        return jsRuntime.Invoke<string>("BitBlazorUI.Utils.getProperty", element, property);
    }


    internal static ValueTask<string[]> BitUtilsGetChildrenAttributes(this IJSRuntime jsRuntime, string containerId, string attribute)
    {
        return jsRuntime.Invoke<string[]>("BitBlazorUI.Utils.getChildrenAttributes", containerId, attribute);
    }


    internal static ValueTask<BoundingClientRect> BitUtilsGetBoundingClientRect(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.Invoke<BoundingClientRect>("BitBlazorUI.Utils.getBoundingClientRect", element);
    }


    internal static ValueTask BitUtilsFocusFirstElement(this IJSRuntime jsRuntime, string elementId, string? selector = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.focusFirstElement", elementId, selector);
    }


    internal static ValueTask<bool> BitUtilsContainsActiveElement(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.containsActiveElement", elementId);
    }


    internal static ValueTask<bool> BitUtilsIsHoverDevice(this IJSRuntime jsRuntime)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.isHoverDevice");
    }


    internal static ValueTask BitUtilsSetupFocusTrap(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupFocusTrap", elementId);
    }


    internal static ValueTask BitUtilsDisposeFocusTrap(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeFocusTrap", elementId);
    }


    internal static ValueTask BitUtilsSaveFocus(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.saveFocus", key);
    }


    internal static ValueTask BitUtilsRestoreFocus(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.restoreFocus", key);
    }


    internal static ValueTask BitUtilsClearSavedFocus(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.clearSavedFocus", key);
    }


    internal static ValueTask BitUtilsPreventDefaultKeys(this IJSRuntime jsRuntime, string elementId, string[] keys)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.preventDefaultKeys", elementId, keys);
    }


    internal static ValueTask BitUtilsDisposePreventDefaultKeys(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposePreventDefaultKeys", elementId);
    }


    internal static ValueTask BitUtilsScrollElementIntoView(this IJSRuntime jsRuntime, string targetElementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollElementIntoView", targetElementId);
    }


    internal static ValueTask BitUtilsScrollToOffset(this IJSRuntime jsRuntime, ElementReference element, double offset, bool horizontal, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollTo", element, offset, horizontal, smooth);
    }


    internal static ValueTask BitUtilsScrollToEnd(this IJSRuntime jsRuntime, ElementReference element, bool horizontal, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollToEnd", element, horizontal, smooth);
    }


    internal static ValueTask BitUtilsScrollToChild(this IJSRuntime jsRuntime, ElementReference element, int index, double extraOffset, bool horizontal, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollToChild", element, index, extraOffset, horizontal, smooth);
    }


    internal static ValueTask BitUtilsRegisterPreventPointerDown(this IJSRuntime jsRuntime, ElementReference element, bool active)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventPointerDown", element, active);
    }


    internal static ValueTask BitUtilsRegisterPreventWheel(this IJSRuntime jsRuntime, ElementReference element, bool active, bool verticalOnly)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventWheel", element, active, verticalOnly);
    }


    internal static ValueTask BitUtilsRegisterPreventKeys(this IJSRuntime jsRuntime, ElementReference element, string[] keys)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventKeys", element, keys);
    }


    internal static ValueTask BitUtilsRegisterPreventShiftWheel(this IJSRuntime jsRuntime, ElementReference element, bool active)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventShiftWheel", element, active);
    }


    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitOverflowMetrics))]
    internal static ValueTask<BitOverflowMetrics?> BitUtilsGetOverflowMetrics(this IJSRuntime jsRuntime, string containerId, string childSelector)
    {
        return jsRuntime.Invoke<BitOverflowMetrics?>("BitBlazorUI.Utils.getOverflowMetrics", containerId, childSelector);
    }


    internal static ValueTask BitUtilsFocusItem(this IJSRuntime jsRuntime, string containerId, string selector, string mode, string? character)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.focusItem", containerId, selector, mode, character);
    }


    internal static ValueTask BitUtilsSelectText(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.selectText", element);
    }


    internal static ValueTask BitUtilsSetStyle(this IJSRuntime jsRuntime, ElementReference element, string key, string value)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setStyle", element, key, value);
    }


    internal static ValueTask<float> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, string scrollerSelector, bool isHidden)
    {
        return jsRuntime.Invoke<float>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden);
    }

    internal static ValueTask<float> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, ElementReference scrollerElement, bool isHidden)
    {
        return jsRuntime.Invoke<float>("BitBlazorUI.Utils.toggleOverflow", scrollerElement, isHidden);
    }
}
