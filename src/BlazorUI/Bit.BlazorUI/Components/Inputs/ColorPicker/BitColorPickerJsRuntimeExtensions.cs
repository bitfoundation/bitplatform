namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtensions
{
    // FastInvoke returns default (null) when the runtime can't service interop or a JSON/JS interop
    // error is swallowed on the in-process (WASM) path. The nullable failure state is preserved (rather
    // than normalized to an empty id) so DisposeAsync can tell a setup that never registered anything on
    // the JS side from one that did, and skip the (now pointless) JS dispose call.
    internal static async ValueTask<string?> BitColorPickerSetup(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, ElementReference saturationPicker, string pointerHandler, string pointerUpHandler)
    {
        const string identifier = "BitBlazorUI.ColorPicker.setup";
        var result = await js.FastInvoke<string>(identifier, obj, saturationPicker, pointerHandler, pointerUpHandler);
        js.ReportIfUnexpectedNull(identifier, result);
        return result;
    }

    internal static ValueTask<bool> BitColorPickerIsEyeDropperSupported(this IJSRuntime js)
    {
        return js.FastInvoke<bool>("BitBlazorUI.ColorPicker.isEyeDropperSupported");
    }

    /// <remarks>
    /// The call is left running until the user answers it: the eyedropper stays open for as long as they
    /// take to find the color, which is easily longer than the one-minute timeout a Blazor Server circuit
    /// applies to interop by default. Passing a cancellation token - even one that never fires - is what
    /// opts the call out of that timeout.
    /// <para>
    /// This is the asynchronous invocation on purpose: the JS side returns a promise, which the fast
    /// in-process path would turn into a fire-and-forget call that never yields the sampled color.
    /// </para>
    /// </remarks>
    internal static ValueTask<string?> BitColorPickerOpenEyeDropper(this IJSRuntime js)
    {
        return js.Invoke<string?>("BitBlazorUI.ColorPicker.openEyeDropper", CancellationToken.None);
    }

    internal static ValueTask BitColorPickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.ColorPicker.dispose", abortControllerId);
    }
}
