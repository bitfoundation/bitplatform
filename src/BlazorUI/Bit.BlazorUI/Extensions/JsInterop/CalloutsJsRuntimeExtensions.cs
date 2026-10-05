using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class CalloutsJsRuntimeExtensions
{
    internal static ValueTask<bool> BitCalloutToggleCallout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        DotNetObjectReference<T> dotnetObj,
        string componentId,
        ElementReference? component,
        string calloutId,
        ElementReference? callout,
        string overlayId,
        bool isCalloutOpen,
        BitResponsiveMode responsiveMode,
        BitDropDirection dropDirection,
        bool isRtl,
        string scrollContainerId,
        int scrollOffset,
        string headerId,
        string footerId,
        bool setCalloutWidth,
        bool fixedCalloutWidth,
        int maxWindowWidth,
        // An optional cap on the scrollable content of the callout, in pixels. It is applied on top of
        // the space the viewport leaves, so it can only ever make the list shorter; zero means the
        // viewport alone decides, which is what the components that do not offer a cap pass.
        int maxHeight = 0,
        // The id of the arrow (beak) element that points at the anchor of the callout, or an empty string
        // for the callouts that show none, which is what every component without an arrow passes.
        string arrowId = "",
        // Extra distance in pixels between the anchor and the callout, on top of the 1px the placement
        // always leaves; zero keeps the callout tucked against its anchor.
        int gap = 0,
        // Keeps a scroll or a resize of the page from dismissing the callout: it is re-anchored to its
        // anchor instead. Another callout opening still takes over from it.
        bool noDismiss = false,
        // The side of the anchor the callout is preferably placed on ("top", "bottom", "start" or "end"),
        // or an empty string to leave the placement entirely to the drop direction.
        string preferredSide = "",
        // How the callout is lined up with the anchor across the side it is placed on ("center" or "end"),
        // or an empty string for the start-edge alignment every component without the choice gets.
        string alignment = "",
        // Keeps the callout on the preferred side even when it does not fit there, instead of flipping it
        // to the opposite one. It has nothing to hold in place without a preferred side.
        bool noFlip = false,
        // The distance in pixels the callout keeps from the edges of the screen, taken off the room every
        // side is measured against; zero lets the callout go right up to them.
        int collisionPadding = 0,
        // The distance in pixels the callout is slid along the axis it is aligned on, inwards from the edge
        // of the component the alignment lined it up with; zero keeps it on that edge, and a centered
        // callout has no edge for it to run from.
        int alignmentOffset = 0,
        // The distance in pixels the arrow is kept away from the corners of the callout, so that it never
        // lands on a rounded one; zero takes the default the placement keeps on its own.
        int arrowPadding = 0,
        // Keeps a scroll or a resize of the page from dismissing the callout, without what noDismiss also
        // takes away: a click outside of it still closes it. It is re-anchored to its anchor instead.
        bool noScrollDismiss = false,
        // The id of the root of the component, which its popup is rendered beside rather than inside. What the
        // consumer declared on the root - the custom properties of Style and Styles.Root, the classes of Class
        // and Classes.Root - is carried into the popup while it is relocated to the body; an empty string
        // carries nothing, which is what a callout opened from inside another one passes.
        string rootId = "") where T : class
    {
        return jsRuntime.Invoke<bool>(
            "BitBlazorUI.Callouts.toggle",
            dotnetObj,
            componentId,
            component,
            calloutId,
            callout,
            overlayId,
            isCalloutOpen,
            responsiveMode,
            dropDirection,
            isRtl,
            scrollContainerId,
            scrollOffset,
            headerId,
            footerId,
            setCalloutWidth,
            fixedCalloutWidth,
            maxWindowWidth,
            maxHeight,
            arrowId,
            gap,
            noDismiss,
            preferredSide,
            alignment,
            noFlip,
            collisionPadding,
            alignmentOffset,
            arrowPadding,
            noScrollDismiss,
            rootId);
    }

    // Re-applies the space the scrollable content of the open callout cannot use, for the parts above
    // it that come and go while it stays open. It does nothing when the given callout is not the open one.
    internal static ValueTask BitCalloutUpdateScrollOffset(this IJSRuntime jsRuntime, string calloutId, int scrollOffset)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.updateScrollOffset", calloutId, scrollOffset);
    }

    // Lays every open callout out again against what it is placed on, with the inputs it was opened with.
    // It is what a callout that is still open and has only moved needs - a context menu brought along to a
    // second right-click, a content that has changed size - since going through the toggle would replay the
    // entry animation of a callout that never went anywhere.
    internal static ValueTask BitCalloutReposition(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.reposition");
    }

    internal static ValueTask BitCalloutClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.clear", calloutId);
    }

    // Hands the keyboard over to an opening BitCallout in one round trip: remembers the element the focus is on,
    // listens for Escape on the page, traps the focus, or puts the callout in the tab order after its trigger -
    // each only where asked for (a null trigger id asks for no tab-out). Returns whether a focus origin was
    // remembered, which it is not for a focus that was on the body.
    internal static ValueTask<bool> BitCalloutSetupKeyboard<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        string calloutId,
        DotNetObjectReference<T> dotnetObj,
        bool captureFocusOrigin,
        bool escape,
        string? escapeTriggerId,
        bool focusTrap,
        string? tabOutTriggerId) where T : class
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Callouts.setupKeyboard",
                                      calloutId, dotnetObj, captureFocusOrigin, escape, escapeTriggerId, focusTrap, tabOutTriggerId);
    }

    // Takes back everything BitCalloutSetupKeyboard may have set up, in one round trip.
    internal static ValueTask BitCalloutDisposeKeyboard(this IJSRuntime jsRuntime, string calloutId, bool forgetFocusOrigin)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.disposeKeyboard", calloutId, forgetFocusOrigin);
    }
}
