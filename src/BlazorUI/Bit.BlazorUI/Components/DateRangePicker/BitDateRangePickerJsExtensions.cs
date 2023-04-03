
namespace Bit.BlazorUI;

internal static class BitDateRangePickerJsExtensions
{
    internal static async Task SetupTimeInput(this IJSRuntime jsRuntime, ElementReference timeInput)
    {
        await jsRuntime.InvokeVoidAsync("BitDateRangePicker.setupTimeInput", timeInput);
    }
}
