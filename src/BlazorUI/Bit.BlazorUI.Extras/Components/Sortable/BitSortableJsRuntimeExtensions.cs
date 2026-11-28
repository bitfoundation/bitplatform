namespace Bit.BlazorUI;

internal static class BitSortableJsRuntimeExtensions
{
    public static ValueTask BitSortableRegister<TItem>(this IJSRuntime jsRuntime,
                                                       ElementReference list,
                                                       DotNetObjectReference<BitSortable<TItem>> dotnetObj,
                                                       BitSortableJsOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Sortable.register", list, dotnetObj, options);
    }

    public static ValueTask BitSortableConfigure(this IJSRuntime jsRuntime, ElementReference list, BitSortableJsOptions options)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Sortable.configure", list, options);
    }

    public static ValueTask BitSortableUnregister(this IJSRuntime jsRuntime, ElementReference list)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Sortable.unregister", list);
    }

    public static ValueTask BitSortableCapture(this IJSRuntime jsRuntime, ElementReference list)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Sortable.capture", list);
    }

    public static ValueTask BitSortablePlay(this IJSRuntime jsRuntime, ElementReference list, int duration)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Sortable.play", list, duration);
    }

    public static ValueTask BitSortableAnnounce(this IJSRuntime jsRuntime, string text)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Sortable.announce", text);
    }
}
