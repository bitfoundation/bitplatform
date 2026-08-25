namespace Bit.BlazorUI;

internal static class BitTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitTimePickerSetup(this IJSRuntime js, ElementReference callout, ElementReference? input, bool trapFocus)
    {
        return js.Invoke<string>("BitBlazorUI.TimePicker.setup", callout, input, trapFocus);
    }

    internal static ValueTask BitTimePickerDispose(this IJSRuntime js, string? abortControllerId)
    {
        return js.InvokeVoid("BitBlazorUI.TimePicker.dispose", abortControllerId);
    }
}
