namespace Bit.BlazorUI;

internal static class BitColorPickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitColorPickerSetup(this IJSRuntime js, DotNetObjectReference<BitColorPicker> obj, ElementReference saturationPicker, string pointerHandler, string pointerUpHandler)
    {
        return js.FastInvoke<string>("BitBlazorUI.ColorPicker.setup", obj, saturationPicker, pointerHandler, pointerUpHandler);
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
