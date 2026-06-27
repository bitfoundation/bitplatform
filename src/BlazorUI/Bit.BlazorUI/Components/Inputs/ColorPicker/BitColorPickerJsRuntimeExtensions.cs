namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtensions
{
    // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
    // error is swallowed on the in-process (WASM) path. The nullable failure state is preserved (rather
    // than normalized to an empty id) so callers can detect a failed setup and dispose the
    // DotNetObjectReference themselves, since the JS dispose path can't own a reference that was never
    // registered.
    internal static async ValueTask<string?> BitColorPickerSetup(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        const string identifier = "BitBlazorUI.ColorPicker.setup";
        var result = await js.FastInvoke<string>(identifier, obj, pointerUpHandler, pointerMoveHandler);
        js.ReportIfUnexpectedNull(identifier, result);
        return result;
    }

    internal static ValueTask BitColorPickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.ColorPicker.dispose", abortControllerId);
    }
}
