using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitCircularTimePickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitCircularTimePickerSetup(this IJSRuntime jSRuntime, DotNetObjectReference<BitCircularTimePicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        return jSRuntime.FastInvoke<string>("BitBlazorUI.CircularTimePicker.setup", obj, pointerUpHandler, pointerMoveHandler);
    }

    internal static ValueTask BitCircularTimePickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.CircularTimePicker.dispose", abortControllerId);
    }
}
