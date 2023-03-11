namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.DatePicker;

public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
