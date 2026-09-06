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
                                                                    int maxPull,
                                                                    bool enabled,
                                                                    DotNetObjectReference<BitPullToRefresh>? dotnetObjectReference)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.setup", id, anchor, loading, scrollerElement, scrollerSelector, trigger, factor, margin, threshold, maxPull, enabled, dotnetObjectReference);
    }

    internal static ValueTask BitPullToRefreshUpdate(this IJSRuntime jsRuntime,
                                                                    string id,
                                                                    ElementReference? scrollerElement,
                                                                    string? scrollerSelector,
                                                                    int trigger,
                                                                    decimal factor,
                                                                    int margin,
                                                                    int threshold,
                                                                    int maxPull,
                                                                    bool enabled)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.update", id, scrollerElement, scrollerSelector, trigger, factor, margin, threshold, maxPull, enabled);
    }

    internal static ValueTask BitPullToRefreshRefresh(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.refresh", id);
    }

    internal static ValueTask BitPullToRefreshDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.dispose", id);
    }
}
