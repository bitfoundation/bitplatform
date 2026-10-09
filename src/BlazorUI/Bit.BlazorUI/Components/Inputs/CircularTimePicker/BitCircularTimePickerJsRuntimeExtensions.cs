namespace Bit.BlazorUI;

internal static class BitCircularTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitCircularTimePickerSetup(this IJSRuntime js,
        DotNetObjectReference<BitCircularTimePicker> obj,
        ElementReference clock,
        ElementReference input,
        ElementReference callout,
        bool modal,
        string pointerDownHandler,
        string pointerMoveHandler,
        string pointerUpHandler,
        string focusOutHandler)
    {
        return js.Invoke<string>("BitBlazorUI.CircularTimePicker.setup", obj, clock, input, callout, modal,
                                 pointerDownHandler, pointerMoveHandler, pointerUpHandler, focusOutHandler);
    }

    internal static ValueTask BitCircularTimePickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.InvokeVoid("BitBlazorUI.CircularTimePicker.dispose", abortControllerId);
    }
}
