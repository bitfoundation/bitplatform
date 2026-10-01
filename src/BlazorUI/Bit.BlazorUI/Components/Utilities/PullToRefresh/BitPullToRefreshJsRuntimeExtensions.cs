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
                                                                    bool noMouse,
                                                                    DotNetObjectReference<BitPullToRefresh>? dotnetObjectReference)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.setup", id, anchor, loading, scrollerElement, scrollerSelector, trigger, factor, margin, threshold, maxPull, enabled, noMouse, dotnetObjectReference);
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
                                                                    bool enabled,
                                                                    bool noMouse)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.update", id, scrollerElement, scrollerSelector, trigger, factor, margin, threshold, maxPull, enabled, noMouse);
    }

    internal static ValueTask BitPullToRefreshRefresh(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.refresh", id);
    }

    internal static ValueTask BitPullToRefreshRelease(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.release", id);
    }

    internal static ValueTask BitPullToRefreshDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.PullToRefresh.dispose", id);
    }
}
