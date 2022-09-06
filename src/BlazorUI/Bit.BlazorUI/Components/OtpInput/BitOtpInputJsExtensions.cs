
namespace Bit.BlazorUI;

internal static class BitOtpInputJsExtensions
{
    internal static async Task<string?> GetPastedData(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<string?>("BitOtpInput.getPastedData");
    }
}
