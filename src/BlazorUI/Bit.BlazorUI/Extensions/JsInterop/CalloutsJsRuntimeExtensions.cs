using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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
        return jsRuntime.FastInvoke<bool>("BitBlazorUI.Callouts.toggle",
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
        return jsRuntime.FastInvokeVoid("BitBlazorUI.Callouts.clear", calloutId);
    }
}
