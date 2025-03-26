namespace Bit.BlazorUI;

internal static class BitCircularTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitCircularTimePickerSetup(this IJSRuntime js, DotNetObjectReference<BitCircularTimePicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        return js.Invoke<string>("BitBlazorUI.CircularTimePicker.setup", obj, pointerUpHandler, pointerMoveHandler);
    }

    internal static ValueTask BitCircularTimePickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.InvokeVoid("BitBlazorUI.CircularTimePicker.dispose", abortControllerId);
    }
}
