using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitBreadcrumbJsRuntimeExtension
{
    internal static async Task ToggleOverflowCallout<T>(this IJSRuntime jsRuntime,
                                                        DotNetObjectReference<BitBreadcrumb<T>> component,
                                                        string wrapperId,
                                                        string dropDownId,
                                                        string calloutId,
                                                        string overlayId,
                                                        bool isCalloutOpen) where T : class
    {
        await jsRuntime.InvokeAsync<string>("BitBreadcrumb.toggleOverflowCallout", component, wrapperId, dropDownId, calloutId, overlayId, isCalloutOpen);
    }
}
