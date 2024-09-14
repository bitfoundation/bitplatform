namespace Bit.BlazorUI;

internal static class BitOtpInputJsRuntimeExtensions
{
    internal static ValueTask BitOtpInputSetup(this IJSRuntime jsRuntime, DotNetObjectReference<BitOtpInput> obj, ElementReference input)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.OtpInput.setup", obj, input);
    }
}
