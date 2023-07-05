namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.DatePicker;

public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}
