
namespace Bit.BlazorUI;

internal static class BitOtpInputJsExtensions
{
    internal static async Task SetupOtpInputPaste(this IJSRuntime jsRuntime, DotNetObjectReference<BitOtpInput> obj, ElementReference otp)
    {
        await jsRuntime.InvokeVoidAsync("BitOtpInput.setupOtpInputPaste", obj, otp);
    }
}
