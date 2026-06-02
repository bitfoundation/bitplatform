namespace Bit.BlazorUI;

internal static class BitScrollablePaneJsRuntimeExtensions
{
    // ScrollablePane.scrollToEnd is a synchronous, null-guarded DOM call (see BitScrollablePane.ts),
    // so it is safe to run through FastInvokeVoid (synchronous on in-process runtimes, async fallback otherwise).
    internal static ValueTask BitScrollablePaneScrollToEnd(this IJSRuntime jsRuntime, ElementReference element)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.ScrollablePane.scrollToEnd", element);
    }
}
