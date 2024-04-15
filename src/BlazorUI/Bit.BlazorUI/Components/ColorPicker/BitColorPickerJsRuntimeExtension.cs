namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtension
{
    internal static async Task<string> BitColorPickerRegisterPointerUp(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string methodName)
    {
        return await js.InvokeAsync<string>("BitBlazorUI.ColorPicker.registerEvent", "pointerup", obj, methodName);
    }

    internal static async Task<string> BitColorPickerRegisterPointerMove(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string methodName)
    {
        return await js.InvokeAsync<string>("BitBlazorUI.ColorPicker.registerEvent", "pointermove", obj, methodName);
    }

    internal static async Task BitColorPickerAbort(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        await jSRuntime.InvokeVoidAsync("BitBlazorUI.ColorPicker.abort", abortControllerId);
    }
}
