namespace Bit.BlazorUI;

internal static class BitCircularTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitCircularTimePickerRegisterPointerUp(this IJSRuntime js, DotNetObjectReference<BitCircularTimePicker> obj, string methodName)
    {
        return js.Invoke<string>("BitBlazorUI.CircularTimePicker.registerEvent", "pointerup", obj, methodName);
    }

    internal static ValueTask<string> BitCircularTimePickerRegisterPointerMove(this IJSRuntime js, DotNetObjectReference<BitCircularTimePicker> obj, string methodName)
    {
        return js.Invoke<string>("BitBlazorUI.CircularTimePicker.registerEvent", "pointermove", obj, methodName);
    }

    internal static ValueTask BitCircularTimePickerAbort(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.InvokeVoid("BitBlazorUI.CircularTimePicker.abort", abortControllerId);
    }
}
