using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitColorPickerJsRuntimeExtensions
{
    internal static ValueTask<string> BitColorPickerSetup(this IJSRuntime jSRuntime, DotNetObjectReference<BitColorPicker> obj, string pointerUpHandler, string pointerMoveHandler)
    {
        return jSRuntime.FastInvoke<string>("BitBlazorUI.ColorPicker.setup", obj, pointerUpHandler, pointerMoveHandler);
    }

    internal static ValueTask BitColorPickerDispose(this IJSRuntime jSRuntime, string? abortControllerId)
    {
        return jSRuntime.FastInvokeVoid("BitBlazorUI.ColorPicker.dispose", abortControllerId);
    }
}
