
namespace Bit.BlazorUI;

internal static class BitOtpInputJsExtensions
{
    internal static async Task SetupOtpInput(this IJSRuntime jsRuntime, DotNetObjectReference<BitOtpInput> obj, ElementReference otp)
    {
        await jsRuntime.InvokeVoidAsync("BitOtpInput.setupOtpInput", obj, otp);
    }
}
