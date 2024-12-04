namespace Bit.BlazorUI;

internal static class BitSwipeTrapJsRuntimeExtensions
{
    internal static ValueTask BitSwipeTrapSetup(this IJSRuntime js,
                                                     string id,
                                                     ElementReference element,
                                                     decimal trigger,
                                                     decimal threshold,
                                                     int step,
                                                     int throttle,
                                                     DotNetObjectReference<BitSwipeTrap>? dotnetObjectReference)
    {
        return js.InvokeVoid("BitBlazorUI.SwipeTrap.setup", id, element, trigger, threshold, step, throttle, dotnetObjectReference);
    }

    internal static ValueTask BitSwipeTrapDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SwipeTrap.dispose", id);
    }
}
