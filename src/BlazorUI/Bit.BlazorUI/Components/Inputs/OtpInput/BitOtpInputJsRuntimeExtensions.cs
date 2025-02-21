using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
internal static class BitOtpInputJsRuntimeExtensions
{
    internal static ValueTask BitOtpInputSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitOtpInput> obj, ElementReference input)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.OtpInput.setup", id, obj, input);
    }

    internal static ValueTask BitOtpInputDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.OtpInput.dispose", id);
    }
}
