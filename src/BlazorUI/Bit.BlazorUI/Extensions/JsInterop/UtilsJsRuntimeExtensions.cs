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


    internal static ValueTask BitUtilsFocusFirstElement(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.focusFirstElement", elementId);
    }


    // Mirrors the popup relationship of a popup component onto the element the user actually reaches: the
    // anchor of a callout is a plain container around the consumer's own trigger, and relationship
    // attributes on a container no screen reader ever lands on are attributes no screen reader ever reads.
    // An empty hasPopup takes the attribute away again, for the popups that are not one of the kinds the
    // property can name.
    internal static ValueTask BitUtilsSyncAriaPopup(this IJSRuntime jsRuntime, string anchorId, string popupId, bool isOpen, string? hasPopup)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.syncAriaPopup", anchorId, popupId, isOpen, hasPopup ?? string.Empty);
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


    // Remembers the element the focus was on when a popup took it over, so the popup can hand the keyboard
    // back to where it came from once it closes.
    internal static ValueTask BitUtilsCaptureFocusOrigin(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.captureFocusOrigin", elementId);
    }


    internal static ValueTask BitUtilsRestoreFocusOrigin(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.restoreFocusOrigin", elementId);
    }


    internal static ValueTask BitUtilsDisposeFocusOrigin(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeFocusOrigin", elementId);
    }


    // Reports the end of the transform transition of an element back to .NET, which is when a surface that
    // slides has actually finished sliding.
    internal static ValueTask BitUtilsSetupTransitionEnd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupTransitionEnd", elementId, dotnetObj);
    }


    internal static ValueTask BitUtilsDisposeTransitionEnd(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeTransitionEnd", elementId);
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


    // The key names the holder of the lock, so that an element several surfaces are holding still at once
    // gets its scrolling back when the last of them lets go rather than when the first one does. Callers that
    // pass none share one anonymous holder, which is the behaviour of a single lock.
    internal static ValueTask<float> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, string scrollerSelector, bool isHidden, string? key = null)
    {
        return jsRuntime.Invoke<float>("BitBlazorUI.Utils.toggleOverflow", scrollerSelector, isHidden, key ?? string.Empty);
    }

    internal static ValueTask<float> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, ElementReference scrollerElement, bool isHidden, string? key = null)
    {
        return jsRuntime.Invoke<float>("BitBlazorUI.Utils.toggleOverflow", scrollerElement, isHidden, key ?? string.Empty);
    }
}
