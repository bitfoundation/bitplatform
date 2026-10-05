namespace Bit.BlazorUI;

internal static class BitTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitTimePickerSetup(this IJSRuntime js, ElementReference callout, ElementReference? input, bool trapFocus, DotNetObjectReference<BitTimePicker> dotnetObj)
    {
        return js.Invoke<string>("BitBlazorUI.TimePicker.setup", callout, input, trapFocus, dotnetObj);
    }

    internal static ValueTask BitTimePickerDispose(this IJSRuntime js, string? abortControllerId)
    {
        return js.InvokeVoid("BitBlazorUI.TimePicker.dispose", abortControllerId);
    }
}
