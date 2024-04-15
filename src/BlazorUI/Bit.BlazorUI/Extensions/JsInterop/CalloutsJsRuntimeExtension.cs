using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class CalloutsJsRuntimeExtension
{
    internal static async Task<bool> ToggleCallout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        DotNetObjectReference<T> dotnetObj,
        string componentId,
        string calloutId,
        bool isCalloutOpen,
        BitResponsiveMode responsiveMode,
        BitDropDirection dropDirection,
        bool isRtl,
        string scrollContainerId,
        int scrollOffset,
        string headerId,
        string footerId,
        bool setCalloutWidth,
        string rootCssClass
        ) where T : class
    {
        return await jsRuntime.InvokeAsync<bool>("BitBlazorUI.Callouts.toggle",
                                                 dotnetObj, componentId, calloutId, isCalloutOpen, responsiveMode, dropDirection,
                                                 isRtl, scrollContainerId, scrollOffset, headerId, footerId, setCalloutWidth,
                                                 rootCssClass);
    }

    internal static async Task ClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.Callouts.clear", calloutId);
    }
}
