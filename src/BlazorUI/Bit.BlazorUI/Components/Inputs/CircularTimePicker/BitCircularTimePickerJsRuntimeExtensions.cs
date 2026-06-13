namespace Bit.BlazorUI;

internal static class BitCircularTimePickerJsRuntimeExtensions
{
    // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
    // error is swallowed on the in-process (WASM) path, so normalize to an empty id to keep callers
    // from treating a failed setup as an initialized abort controller.
    internal static async ValueTask<string> BitCircularTimePickerSetup(this IJSRuntime js, DotNetObjectReference<BitCircularTimePicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        const string identifier = "BitBlazorUI.CircularTimePicker.setup";
        var result = await js.FastInvoke<string>(identifier, obj, pointerUpHandler, pointerMoveHandler);
        js.ReportIfUnexpectedNull(identifier, result);
        return result ?? string.Empty;
    }

    internal static ValueTask BitCircularTimePickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.CircularTimePicker.dispose", abortControllerId);
    }
}
