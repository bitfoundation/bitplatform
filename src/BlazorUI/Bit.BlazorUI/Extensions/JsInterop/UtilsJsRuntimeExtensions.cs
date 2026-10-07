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

    // Focuses the trigger inside the given container unless the focus is already in it; see Utils.focusClickedTrigger.
    internal static ValueTask BitUtilsFocusClickedTrigger(this IJSRuntime jsRuntime, string containerId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.focusClickedTrigger", containerId);
    }


    // Mirrors the popup relationship of a popup component onto the element the user actually reaches: the
    // anchor of a callout is a plain container around the consumer's own trigger, and relationship
    // attributes on a container no screen reader ever lands on are attributes no screen reader ever reads.
    // An empty hasPopup takes the attribute away again, for the popups that are not one of the kinds the
    // property can name. Returns whether the attributes landed on a trigger inside the anchor rather than
    // on the anchor itself.
    internal static ValueTask<bool> BitUtilsSyncAriaPopup(this IJSRuntime jsRuntime, string anchorId, string popupId, bool isOpen, string? hasPopup)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.syncAriaPopup", anchorId, popupId, isOpen, hasPopup ?? string.Empty);
    }


    // Stops watching the anchor BitUtilsSyncAriaPopup keeps the relationship on the current trigger of.
    internal static ValueTask BitUtilsDisposeAriaPopup(this IJSRuntime jsRuntime, string anchorId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeAriaPopup", anchorId);
    }


    // Mirrors the relationship a tooltip declares onto the element the reader actually lands on: a tooltip
    // renders the consumer's anchor inside a plain container of its own, and a relationship declared on a
    // container that is neither focusable nor interactive is one no screen reader ever reads. An empty
    // attribute takes the mirrored one away again.
    internal static ValueTask BitUtilsSyncAriaDescription(this IJSRuntime jsRuntime, string rootId, string tooltipId, string attribute)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.syncAriaDescription", rootId, tooltipId, attribute);
    }


    // Registers the Escape a shown tooltip takes for itself - pressed inside it, or anywhere on the page while the
    // pointer rests on it - and mirrors its relationship onto the anchor in the same call; see Utils.setupTooltip.
    internal static ValueTask BitUtilsSetupTooltip<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string rootId, string tooltipId, string attribute, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupTooltip", rootId, tooltipId, attribute, dotnetObj);
    }


    internal static ValueTask BitUtilsDisposeTooltip(this IJSRuntime jsRuntime, string rootId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeTooltip", rootId);
    }


    internal static ValueTask<bool> BitUtilsContainsActiveElement(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.containsActiveElement", elementId);
    }


    internal static ValueTask<bool> BitUtilsPrefersReducedMotion(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.prefersReducedMotion", element);
    }


    internal static ValueTask<bool> BitUtilsIsHoverDevice(this IJSRuntime jsRuntime)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.isHoverDevice");
    }


    // `anchorId` is the element around the container that catches the focus a press on the overlay moves
    // and hands it on into the container; see Utils.setupFocusTrap.
    internal static ValueTask BitUtilsSetupFocusTrap(this IJSRuntime jsRuntime, string elementId, string? anchorId = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupFocusTrap", elementId, anchorId);
    }


    internal static ValueTask BitUtilsDisposeFocusTrap(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeFocusTrap", elementId);
    }


    // Answers an Escape pressed inside a surface through the OnEscape callback, only when nothing inside it took
    // the key first (an IME composition, a control, a component whose own popup is open, a surface nested inside
    // it); see Utils.setupSurfaceEscape.
    internal static ValueTask BitUtilsSetupSurfaceEscape<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupSurfaceEscape", elementId, dotnetObj);
    }


    internal static ValueTask BitUtilsDisposeSurfaceEscape(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeSurfaceEscape", elementId);
    }


    // Hands the keyboard back to the page around the trigger when Tab leaves either end of a popup that is
    // relocated to the body, and reports it through the OnTabOut callback; see Utils.setupTabOut.
    internal static ValueTask BitUtilsSetupTabOut<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, string triggerId, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupTabOut", elementId, triggerId, dotnetObj);
    }


    internal static ValueTask BitUtilsDisposeTabOut(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeTabOut", elementId);
    }


    // Reports Escape pressed inside an open callout through the OnEscape callback, unless a callout opened from
    // inside it is the innermost open one and so the one the key belongs to; see Utils.setupEscape. With a
    // triggerId, an Escape pressed anywhere outside the callout and that trigger is reported as well.
    internal static ValueTask BitUtilsSetupEscape<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, DotNetObjectReference<T> dotnetObj, string? triggerId = null) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupEscape", elementId, dotnetObj, triggerId);
    }


    internal static ValueTask BitUtilsDisposeEscape(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeEscape", elementId);
    }


    // Reports, through the OnEscapeVerdict callback, as each Escape goes down inside the element, whether it
    // belongs to something in there - an open popup of a component in its content, an IME composition, a
    // surface nested inside it - rather than to the surface itself; see Utils.setupEscapeGuard.
    internal static ValueTask BitUtilsSetupEscapeGuard<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.setupEscapeGuard", elementId, dotnetObj);
    }


    internal static ValueTask BitUtilsDisposeEscapeGuard(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.disposeEscapeGuard", elementId);
    }


    // Calls OnEscape for each Escape pressed inside the element that nothing inside it had the better claim to -
    // an open dropdown or menu opened from inside it, an input method composing, a control that prevented the
    // key's default; see Utils.watchEscape.
    internal static ValueTask BitUtilsWatchEscape<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.watchEscape", elementId, dotnetObj);
    }

    internal static ValueTask BitUtilsUnwatchEscape(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.unwatchEscape", elementId);
    }


    // Calls OnEscape for each Escape pressed while a layer that covers the page is open, wherever the focus is,
    // as long as nothing had the better claim to the key and the layer is the topmost one; see
    // Utils.watchLayerEscape.
    internal static ValueTask BitUtilsWatchLayerEscape<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime, string elementId, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.watchLayerEscape", elementId, dotnetObj);
    }

    internal static ValueTask BitUtilsUnwatchLayerEscape(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.unwatchLayerEscape", elementId);
    }


    // Resolves once the exit animation of the element and its direct children has played out, bounded by a
    // timeout; see Utils.waitForAnimations.
    internal static ValueTask BitUtilsWaitForAnimations(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.waitForAnimations", elementId);
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


    // The same hand-back, reporting whether the focus was taken care of: false when there was no origin to
    // hand it back to (the focus was on the body when the popup opened) or the origin has left the page, which
    // leaves the caller the focus to place itself.
    internal static ValueTask<bool> BitUtilsTryRestoreFocusOrigin(this IJSRuntime jsRuntime, string elementId)
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Utils.restoreFocusOrigin", elementId);
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
    // A focus still inside the element named by scopeId - a popup that stays in the page while it closes - counts
    // as lost too.
    internal static ValueTask BitUtilsRestoreFocus(this IJSRuntime jsRuntime, string key, bool onlyWhenLost = true, string? scopeId = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.restoreFocus", key, onlyWhenLost, scopeId);
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


    internal static ValueTask BitUtilsScrollElementIntoView(this IJSRuntime jsRuntime, string targetElementId, bool focus = false)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollElementIntoView", targetElementId, focus);
    }


    internal static ValueTask BitUtilsScrollToOffset(this IJSRuntime jsRuntime, ElementReference element, double offset, bool horizontal, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollTo", element, offset, horizontal, smooth);
    }


    internal static ValueTask BitUtilsScrollToEnd(this IJSRuntime jsRuntime, ElementReference element, bool horizontal, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollToEnd", element, horizontal, smooth);
    }


    internal static ValueTask BitUtilsScrollToChild(this IJSRuntime jsRuntime, ElementReference element, ElementReference container, int index, double extraOffset, bool horizontal, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.scrollToChild", element, container, index, extraOffset, horizontal, smooth);
    }


    internal static ValueTask BitUtilsRegisterButtonKeys(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerButtonKeys", element);
    }


    internal static ValueTask BitUtilsRegisterPreventPointerDown(this IJSRuntime jsRuntime, ElementReference element, bool active, int clickThreshold = 0, string? clickAxis = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventPointerDown", element, active, clickThreshold, clickAxis);
    }


    internal static ValueTask BitUtilsRegisterPreventWheel(this IJSRuntime jsRuntime, ElementReference element, bool active, bool verticalOnly)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventWheel", element, active, verticalOnly);
    }


    internal static ValueTask BitUtilsRegisterPreventKeys(this IJSRuntime jsRuntime, ElementReference element, string[] keys)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerPreventKeys", element, keys);
    }


    internal static ValueTask BitUtilsRegisterNavigationKeys<T>(this IJSRuntime jsRuntime, ElementReference element, string[] keys, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Utils.registerNavigationKeys", element, keys, dotnetObj);
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
