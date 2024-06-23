namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DatePicker;

public class BitDatePickerValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
