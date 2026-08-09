namespace Bit.BlazorUI;

internal static class BitCircularTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitCircularTimePickerSetup(this IJSRuntime js,
        DotNetObjectReference<BitCircularTimePicker> obj,
        ElementReference clock,
        ElementReference input,
        string pointerDownHandler,
        string pointerMoveHandler,
        string pointerUpHandler)
    {
        return js.Invoke<string>("BitBlazorUI.CircularTimePicker.setup", obj, clock, input, pointerDownHandler, pointerMoveHandler, pointerUpHandler);
    }

    internal static ValueTask BitCircularTimePickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.InvokeVoid("BitBlazorUI.CircularTimePicker.dispose", abortControllerId);
    }
}
