namespace Bit.BlazorUI;

internal static class BitOtpInputJsExtensions
{
    internal static async Task BitOtpInputSetup(this IJSRuntime jsRuntime, DotNetObjectReference<BitOtpInput> obj, ElementReference input)
    {
        await jsRuntime.InvokeVoidAsync("BitBlazorUI.OtpInput.setup", obj, input);
    }
}
