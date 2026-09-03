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


    // Mirrors the popup relationship of a popup component onto the element the user actually reaches: the
    // anchor of a callout is a plain container around the consumer's own trigger, and relationship
    // attributes on a container no screen reader ever lands on are attributes no screen reader ever reads.
    // An empty hasPopup takes the attribute away again, for the popups that are not one of the kinds the
    // property can name.
    internal static ValueTask BitUtilsSyncAriaPopup(this IJSRuntime jsRuntime, string anchorId, string popupId, bool isOpen, string? hasPopup)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.syncAriaPopup", anchorId, popupId, isOpen, hasPopup ?? string.Empty);
    }


    // Mirrors the relationship a tooltip declares onto the element the reader actually lands on: a tooltip
    // renders the consumer's anchor inside a plain container of its own, and a relationship declared on a
    // container that is neither focusable nor interactive is one no screen reader ever reads. An empty
    // attribute takes the mirrored one away again.
    internal static ValueTask BitUtilsSyncAriaDescription(this IJSRuntime jsRuntime, string rootId, string tooltipId, string attribute)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.syncAriaDescription", rootId, tooltipId, attribute);
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


    // Records the element the focus is on right now under the given key, so that a popup which is about to
    // take the focus over can hand it back to whatever opened it once it closes.
    internal static ValueTask BitUtilsStoreFocus(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.storeFocus", key);
    }


    // Hands the focus back to the element stored under the given key and forgets it. With onlyWhenLost the
    // focus is only handed back while nothing else holds it, which after the popup was taken out of the page
    // is the case the restore exists for: a focus that has since moved elsewhere belongs to whoever moved it.
    internal static ValueTask BitUtilsRestoreFocus(this IJSRuntime jsRuntime, string key, bool onlyWhenLost = true)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.restoreFocus", key, onlyWhenLost);
    }


    // Drops the element stored under the given key without focusing it, for a component disposed while its
    // popup is still open.
    internal static ValueTask BitUtilsForgetFocus(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.forgetFocus", key);
    }


    // Stops the given element (the page itself when no selector is given) from scrolling while the popup
    // registered under the given key is open. The locks are counted, so a page held by more than one popup
    // is only handed back once the last of them lets go, and the room the scrollbar took is added back as
    // padding so that taking it away does not shift the page sideways.
    internal static ValueTask BitUtilsLockScroll(this IJSRuntime jsRuntime, string key, string? selector = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.lockScroll", key, selector);
    }

    // The same hold, taken on an element the caller already has a reference to rather than on one named by a
    // selector - the scroller of an application shell, which no selector of the consumer's is needed to find.
    internal static ValueTask BitUtilsLockScroll(this IJSRuntime jsRuntime, string key, ElementReference scroller)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.lockScroll", key, scroller);
    }


    // Releases the scroll lock held under the given key.
    internal static ValueTask BitUtilsUnlockScroll(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.unlockScroll", key);
    }


    // Hands the wheel and the touch drag that land on the popup registered under the given key to the given
    // scroller, for the popups that cover the page without holding it: their layer is fixed to the viewport,
    // so a gesture on it is chained to the document rather than to the region an application shell scrolls.
    internal static ValueTask BitUtilsForwardScroll(this IJSRuntime jsRuntime, string key, string rootId, string? selector = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.forwardScroll", key, rootId, selector);
    }

    // The same forwarding, aimed at an element the caller already has a reference to rather than at one named
    // by a selector - the scroller of an application shell, first of all.
    internal static ValueTask BitUtilsForwardScroll(this IJSRuntime jsRuntime, string key, string rootId, ElementReference scroller)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.forwardScroll", key, rootId, scroller);
    }


    // Takes back the gesture forwarding registered under the given key.
    internal static ValueTask BitUtilsStopForwardScroll(this IJSRuntime jsRuntime, string key)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.stopForwardScroll", key);
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


    // The key is the component holding the scroller, so that the hold is counted alongside every other
    // popup holding the same one: a component letting go of a scroller another one is still holding would
    // otherwise hand back a page that is meant to stay held.
    // The compensation is opt-in so that the callers that have always let the page shift by the width of
    // the scrollbar it took away carry on doing exactly that; the ones that ask for it get the room back
    // as padding, the way the counted lock above gives it back.
    internal static ValueTask<float> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, string key, string scrollerSelector, bool isHidden, bool compensate = false)
    {
        return jsRuntime.Invoke<float>("BitBlazorUI.Utils.toggleOverflow", key, scrollerSelector, isHidden, compensate);
    }

    internal static ValueTask<float> BitUtilsToggleOverflow(this IJSRuntime jsRuntime, string key, ElementReference scrollerElement, bool isHidden, bool compensate = false)
    {
        return jsRuntime.Invoke<float>("BitBlazorUI.Utils.toggleOverflow", key, scrollerElement, isHidden, compensate);
    }
}
