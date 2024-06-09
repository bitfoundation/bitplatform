namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitColorPickerRegisterPointerUp(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string methodName)
    {
        return js.InvokeAsync<string>("BitBlazorUI.ColorPicker.registerEvent", "pointerup", obj, methodName);
    }

    internal static ValueTask<string> BitColorPickerRegisterPointerMove(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string methodName)
    {
        return js.InvokeAsync<string>("BitBlazorUI.ColorPicker.registerEvent", "pointermove", obj, methodName);
    }

    internal static ValueTask BitColorPickerAbort(this IJSRuntime jSRuntime, string? abortControllerId, bool dispose = false)
    {
        return jSRuntime.InvokeVoidAsync("BitBlazorUI.ColorPicker.abort", abortControllerId, dispose);
    }
}
