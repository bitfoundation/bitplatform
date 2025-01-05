namespace Bit.BlazorUI;

internal static class BitOtpInputJsRuntimeExtensions
{
    internal static ValueTask BitOtpInputSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitOtpInput> obj, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.OtpInput.setup", id, obj, input);
    }

    internal static ValueTask BitOtpInputDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.OtpInput.dispose", id);
    }
}
