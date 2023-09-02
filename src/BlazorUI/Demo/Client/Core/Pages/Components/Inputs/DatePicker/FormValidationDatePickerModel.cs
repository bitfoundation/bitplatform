namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DatePicker;

public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
