namespace Bit.BlazorUI;

internal static class BitOtpInputJsRuntimeExtensions
{
    internal static ValueTask BitOtpInputSetup(this IJSRuntime jsRuntime, string id, DotNetObjectReference<BitOtpInput> obj, ElementReference root, bool smsAutoFill)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.OtpInput.setup", id, obj, root, smsAutoFill);
    }

    internal static ValueTask BitOtpInputBlur(this IJSRuntime jsRuntime, ElementReference root)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.OtpInput.blur", root);
    }

    internal static ValueTask BitOtpInputDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.FastInvokeVoid("BitBlazorUI.OtpInput.dispose", id);
    }
}
