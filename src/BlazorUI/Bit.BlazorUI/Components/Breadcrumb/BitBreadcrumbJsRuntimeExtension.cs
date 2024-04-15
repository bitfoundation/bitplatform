namespace Bit.BlazorUI;

internal static class BitBreadcrumbJsRuntimeExtension
{
    internal static async Task BitBreadcrumbToggleOverflowCallout<T>(this IJSRuntime jsRuntime,
                                                                     DotNetObjectReference<BitBreadcrumb<T>> component,
                                                                     string wrapperId,
                                                                     string dropdownId,
                                                                     string calloutId,
                                                                     string overlayId,
                                                                     bool isCalloutOpen) where T : class
    {
        await jsRuntime.InvokeAsync<string>("BitBlazorUI.Breadcrumb.toggleOverflowCallout",
                                            component,
                                            wrapperId,
                                            dropdownId,
                                            calloutId,
                                            overlayId,
                                            isCalloutOpen);
    }
}
