using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitBreadGroupJsRuntimeExtension
{
    internal static async Task ToggleOverflowCallout(this IJSRuntime jsRuntime, DotNetObjectReference<BitBreadGroup> component,
        string wrapperId,
        string dropDownId,
        string calloutId,
        string overlayId,
        bool isCalloutOpen)
    {
        await jsRuntime.InvokeAsync<string>("BitBreadGroup.toggleOverflowCallout", component, wrapperId, dropDownId, calloutId, overlayId, isCalloutOpen);
    }
}
