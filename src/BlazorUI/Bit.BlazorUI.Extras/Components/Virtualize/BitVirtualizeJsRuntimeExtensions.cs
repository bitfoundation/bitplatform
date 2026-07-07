namespace Bit.BlazorUI;

internal static class BitVirtualizeJsRuntimeExtensions
{
    public static ValueTask<BitVirtualizeMetrics?> BitVirtualizeSetup<T>(this IJSRuntime jsRuntime,
                                                                              string id,
                                                                              ElementReference rootElement,
                                                                              bool horizontal,
                                                                              DotNetObjectReference<BitVirtualize<T>> dotnetObj)
    {
        return jsRuntime.Invoke<BitVirtualizeMetrics?>("BitBlazorUI.Virtualize.setup", id, rootElement, horizontal, dotnetObj);
    }

    public static ValueTask BitVirtualizeSyncMeasurements(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Virtualize.syncMeasurements", id);
    }

    public static ValueTask BitVirtualizeUpdateSticky(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Virtualize.updateSticky", id);
    }

    public static ValueTask BitVirtualizeScrollToOffset(this IJSRuntime jsRuntime, string id, double offset, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Virtualize.scrollToOffset", id, offset, smooth);
    }

    public static ValueTask BitVirtualizeAdjustScroll(this IJSRuntime jsRuntime, string id, double delta)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Virtualize.adjustScroll", id, delta);
    }

    public static ValueTask BitVirtualizeDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Virtualize.dispose", id);
    }
}
