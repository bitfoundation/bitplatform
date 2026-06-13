namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtensions
{
    // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
    // error is swallowed on the in-process (WASM) path, so normalize to an empty id to keep callers
    // from treating a failed setup as an initialized abort controller.
    internal static async ValueTask<string> BitColorPickerSetup(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        const string identifier = "BitBlazorUI.ColorPicker.setup";
        var result = await js.FastInvoke<string>(identifier, obj, pointerUpHandler, pointerMoveHandler);
        js.ReportIfUnexpectedNull(identifier, result);
        return result ?? string.Empty;
    }

    internal static ValueTask BitColorPickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.ColorPicker.dispose", abortControllerId);
    }
}
