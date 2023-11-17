using Bit.BlazorUI;

namespace Microsoft.JSInterop;

internal static class BitColorPickerJsRuntimeExtension
{
    internal static async Task<string> RegisterPointerUp(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string methodName)
    {
        return await js.InvokeAsync<string>("BitColorPicker.register", "pointerup", obj, methodName);
    }

    internal static async Task<string> RegisterPointerMove(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string methodName)
    {
        return await js.InvokeAsync<string>("BitColorPicker.register", "pointermove", obj, methodName);
    }

    internal static async Task AbortProcedure(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        await jSRuntime.InvokeVoidAsync("BitColorPicker.abort", abortControllerId);
    }
}
