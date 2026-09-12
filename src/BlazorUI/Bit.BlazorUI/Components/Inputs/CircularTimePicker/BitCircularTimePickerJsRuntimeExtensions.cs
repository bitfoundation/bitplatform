namespace Bit.BlazorUI;

internal static class BitCircularTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitCircularTimePickerSetup(this IJSRuntime js,
        DotNetObjectReference<BitCircularTimePicker> obj,
        ElementReference clock,
        ElementReference input,
        ElementReference callout,
        bool dismissOnFocusOut,
        string pointerDownHandler,
        string pointerMoveHandler,
        string pointerUpHandler,
        string focusOutHandler)
    {
        return js.FastInvoke<string>("BitBlazorUI.CircularTimePicker.setup", obj, clock, input, callout, dismissOnFocusOut,
                                     pointerDownHandler, pointerMoveHandler, pointerUpHandler, focusOutHandler);
    }

    internal static ValueTask BitCircularTimePickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.CircularTimePicker.dispose", abortControllerId);
    }
}
