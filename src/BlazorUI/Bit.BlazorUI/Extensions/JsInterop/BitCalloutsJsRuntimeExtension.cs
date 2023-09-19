using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class BitCalloutsJsRuntimeExtension
{
    internal static async Task ToggleCallout<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        this IJSRuntime jsRuntime,
        DotNetObjectReference<T> dotnetObj,
        string componentId,
        string calloutId,
        bool isCalloutOpen,
        bool isResponsive,
        BitDropDirection dropDirection,
        bool isRtl,
        string scrollContainerId,
        int scrollOffset
        ) where T : class
    {
        await jsRuntime.InvokeVoidAsync("BitCallouts.toggle",
            dotnetObj, componentId, calloutId, isCalloutOpen, isResponsive, dropDirection, isRtl, scrollContainerId, scrollOffset);
    }

    internal static async Task ClearCallout(this IJSRuntime jsRuntime, string calloutId)
    {
        await jsRuntime.InvokeVoidAsync("BitCallouts.clear", calloutId);
    }
}
