namespace Bit.BlazorUI;

internal static class BitPullToRefreshJsRuntimeExtensions
{
    internal static ValueTask BitPullToRefreshSetup(this IJSRuntime jsRuntime,
                                                                    string id,
                                                                    ElementReference? anchor,
                                                                    ElementReference loading,
                                                                    ElementReference? scrollerElement,
                                                                    string? scrollerSelector,
                                                                    int trigger,
                                                                    decimal factor,
                                                                    int margin,
                                                                    int threshold,
                                                                    DotNetObjectReference<BitPullToRefresh> dotnetObjectReference)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.PullToRefresh.setup", id, anchor, loading, scrollerElement, scrollerSelector, trigger, factor, margin, threshold, dotnetObjectReference);
    }

    // Deliberately not on the FastInvoke path: FastInvokeVoid swallows JSException on the in-process (WASM)
    // runtime, which would hide a failed JS dispose. BitPullToRefresh.DisposeAsync relies on that exception
    // surfacing to know the JS side never took ownership of the DotNetObjectReference and must release it
    // itself, so the dispose call has to use the regular async path where the failure propagates.
    internal static ValueTask BitPullToRefreshDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.dispose", id);
    }
}
