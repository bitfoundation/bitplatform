using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitBreadListJsRuntimeExtension
{
    internal static async Task ToggleOverflowCallout<T>(this IJSRuntime jsRuntime, DotNetObjectReference<BitBreadList<T>> component,
        string wrapperId,
        string dropDownId,
        string calloutId,
        string overlayId,
        bool isCalloutOpen)
    {
        await jsRuntime.InvokeAsync<string>("BitBreadList.toggleOverflowCallout", component, wrapperId, dropDownId, calloutId, overlayId, isCalloutOpen);
    }
}
