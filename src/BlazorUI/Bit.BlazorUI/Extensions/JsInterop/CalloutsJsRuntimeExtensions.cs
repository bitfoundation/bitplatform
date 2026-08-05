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
        int maxHeight = 0) where T : class
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
            maxHeight);
    }

    internal static ValueTask BitCalloutClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.clear", calloutId);
    }
}
