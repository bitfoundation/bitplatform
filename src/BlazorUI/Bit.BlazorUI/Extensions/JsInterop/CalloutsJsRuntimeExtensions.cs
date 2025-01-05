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
        bool isCalloutOpen,
        BitResponsiveMode responsiveMode,
        BitDropDirection dropDirection,
        bool isRtl,
        string scrollContainerId,
        int scrollOffset,
        string headerId,
        string footerId,
        bool setCalloutWidth,
        int maxWidth = 0) where T : class
    {
        return jsRuntime.Invoke<bool>("BitBlazorUI.Callouts.toggle",
                                      dotnetObj,
                                      componentId,
                                      component,
                                      calloutId,
                                      callout,
                                      isCalloutOpen,
                                      responsiveMode,
                                      dropDirection,
                                      isRtl,
                                      scrollContainerId,
                                      scrollOffset,
                                      headerId,
                                      footerId,
                                      setCalloutWidth,
                                      maxWidth);
    }

    internal static ValueTask BitCalloutClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Callouts.clear", calloutId);
    }
}
