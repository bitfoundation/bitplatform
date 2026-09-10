namespace Bit.BlazorUI;

internal static class BitInfiniteScrollingJsRuntimeExtensions
{
    public static ValueTask BitInfiniteScrollingSetup<T>(this IJSRuntime jsRuntime,
                                                              string id,
                                                              string? scrollerSelector,
                                                              ElementReference rootElement,
                                                              ElementReference lastElement,
                                                              decimal? threshold,
                                                              string? rootMargin,
                                                              bool horizontal,
                                                              bool autoLoad,
                                                              DotNetObjectReference<BitInfiniteScrolling<T>> dotnetObj)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.setup", id, scrollerSelector, rootElement, lastElement, threshold, rootMargin, horizontal, autoLoad, dotnetObj);
    }

    public static ValueTask BitInfiniteScrollingReobserve(this IJSRuntime jsRuntime,
                                                               string id,
                                                               ElementReference lastElement)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.reobserve", id, lastElement);
    }

    public static ValueTask BitInfiniteScrollingUnobserve(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.unobserve", id);
    }

    public static ValueTask BitInfiniteScrollingPrepareScroll(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.prepareScroll", id);
    }

    public static ValueTask BitInfiniteScrollingRestoreScroll(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.restoreScroll", id);
    }

    public static ValueTask BitInfiniteScrollingScrollTo(this IJSRuntime jsRuntime, string id, bool toEnd, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.scrollTo", id, toEnd, smooth);
    }

    public static ValueTask BitInfiniteScrollingScrollToOffset(this IJSRuntime jsRuntime, string id, double offset, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.scrollToOffset", id, offset, smooth);
    }

    public static ValueTask<double> BitInfiniteScrollingGetScrollOffset(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.Invoke<double>("BitBlazorUI.InfiniteScrolling.getScrollOffset", id);
    }

    public static ValueTask BitInfiniteScrollingDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.InfiniteScrolling.dispose", id);
    }
}
