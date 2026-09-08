namespace Bit.BlazorUI;

internal static class BitSwipeTrapJsRuntimeExtensions
{
    internal static ValueTask BitSwipeTrapSetup(this IJSRuntime js,
                                                     string id,
                                                     ElementReference element,
                                                     decimal trigger,
                                                     decimal triggerVelocity,
                                                     decimal threshold,
                                                     int throttle,
                                                     BitSwipeOrientation orientationLock,
                                                     bool touchOnly,
                                                     string? skipSelector,
                                                     DotNetObjectReference<BitSwipeTrap>? dotnetObjectReference)
    {
        return js.InvokeVoid("BitBlazorUI.SwipeTrap.setup", id, element, trigger, triggerVelocity, threshold, throttle, orientationLock, touchOnly, skipSelector, dotnetObjectReference);
    }

    internal static ValueTask BitSwipeTrapDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.SwipeTrap.dispose", id);
    }
}
