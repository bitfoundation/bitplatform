using System;
using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitBreadcrumbJsRuntimeExtension
{
    internal static async Task ToggleOverflowCallout(this IJSRuntime jsRuntime, DotNetObjectReference<BitBreadcrumb> component,
        string wrapperId,
        string dropDownId,
        string calloutId,
        string overlayId,
        bool isCalloutOpen)
    {
        await jsRuntime.InvokeAsync<string>("BitBreadcrumb.toggleOverflowCallout", component, wrapperId, dropDownId, calloutId, overlayId, isCalloutOpen);
    }
}
