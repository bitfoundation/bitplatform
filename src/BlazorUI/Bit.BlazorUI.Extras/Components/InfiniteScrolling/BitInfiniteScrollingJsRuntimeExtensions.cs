namespace Bit.BlazorUI;

internal static class BitInfiniteScrollingJsRuntimeExtensions
{
    public static ValueTask BitInfiniteScrollingSetup<T>(this IJSRuntime jsRuntime,
                                                              string id,
                                                              string? scrollerSelector,
                                                              ElementReference rootElement,
                                                              ElementReference lastElement,
                                                              decimal? threshold,
                                                              DotNetObjectReference<BitInfiniteScrolling<T>> dotnetObj)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.InfiniteScrolling.setup", id, scrollerSelector, rootElement, lastElement, threshold, dotnetObj);
    }

    public static ValueTask BitInfiniteScrollingReobserve(this IJSRuntime jsRuntime,
                                                               string id,
                                                               ElementReference lastElement)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.InfiniteScrolling.reobserve", id, lastElement);
    }

    public static ValueTask BitInfiniteScrollingDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.InfiniteScrolling.dispose", id);
    }
}
