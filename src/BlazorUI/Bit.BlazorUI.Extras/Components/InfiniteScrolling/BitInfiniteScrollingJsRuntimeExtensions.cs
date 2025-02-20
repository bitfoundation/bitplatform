using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitInfiniteScrollingJsRuntimeExtensions
{
    public static ValueTask BitInfiniteScrollingSetup<T>(this IJSRuntime jsRuntime,
                                                              string id,
                                                              string? scrollerSelector,
                                                              ElementReference rootElement,
                                                              ElementReference lastElement,
                                                              DotNetObjectReference<BitInfiniteScrolling<T>> dotnetObj)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.InfiniteScrolling.setup", id, scrollerSelector, rootElement, lastElement, dotnetObj);
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
