
namespace Bit.BlazorUI;

internal static class BitDatePickerJsExtensions
{
    internal static async Task SetupTimeInputDatePicker(this IJSRuntime jsRuntime, ElementReference timeInput)
    {
        await jsRuntime.InvokeVoidAsync("BitDatePicker.setupTimeInput", timeInput);
    }
}
