namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitColorPickerSetup(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        return js.Invoke<string>("BitBlazorUI.ColorPicker.setup", obj, pointerUpHandler, pointerMoveHandler);
    }

    internal static ValueTask BitColorPickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.InvokeVoid("BitBlazorUI.ColorPicker.dispose", abortControllerId);
    }
}
