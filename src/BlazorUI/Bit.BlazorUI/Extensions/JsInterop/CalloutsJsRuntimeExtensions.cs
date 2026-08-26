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
        string preferredSide = "") where T : class
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
            preferredSide);
    }

    // Re-applies the space the scrollable content of the open callout cannot use, for the parts above
    // it that come and go while it stays open. It does nothing when the given callout is not the open one.
    internal static ValueTask BitCalloutUpdateScrollOffset(this IJSRuntime jsRuntime, string calloutId, int scrollOffset)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.updateScrollOffset", calloutId, scrollOffset);
    }

    internal static ValueTask BitCalloutClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.clear", calloutId);
    }
}
