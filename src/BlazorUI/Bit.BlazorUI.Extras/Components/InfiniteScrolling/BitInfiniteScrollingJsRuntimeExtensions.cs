﻿namespace Bit.BlazorUI;

internal static class BitInfiniteScrollingJsRuntimeExtensions
{
    public static ValueTask BitInfiniteScrollingSetup<T>(this IJSRuntime jsRuntime,
                                                              string id,
                                                              string? scrollerSelector,
                                                              ElementReference rootElement,
                                                              ElementReference lastElement,
                                                              DotNetObjectReference<BitInfiniteScrolling<T>> dotnetObj)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.InfiniteScrolling.setup", id, scrollerSelector, rootElement, lastElement, dotnetObj);
    }

    public static ValueTask BitInfiniteScrollingReobserve(this IJSRuntime jsRuntime,
                                                               string id,
                                                               ElementReference lastElement)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.InfiniteScrolling.reobserve", id, lastElement);
    }

    public static ValueTask BitInfiniteScrollingDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoidAsync("BitBlazorUI.InfiniteScrolling.dispose", id);
    }
}