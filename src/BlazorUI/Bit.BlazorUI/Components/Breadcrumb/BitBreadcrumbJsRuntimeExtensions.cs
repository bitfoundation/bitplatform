namespace Bit.BlazorUI;

internal static class BitBreadcrumbJsRuntimeExtensions
{
    internal static ValueTask BitBreadcrumbToggleOverflowCallout<T>(this IJSRuntime jsRuntime,
                                                                         DotNetObjectReference<BitBreadcrumb<T>> component,
                                                                         string wrapperId,
                                                                         string dropdownId,
                                                                         string calloutId,
                                                                         string overlayId,
                                                                         bool isCalloutOpen) where T : class
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.Breadcrumb.toggleOverflowCallout",
                                         component,
                                         wrapperId,
                                         dropdownId,
                                         calloutId,
                                         overlayId,
                                         isCalloutOpen);
    }
}
