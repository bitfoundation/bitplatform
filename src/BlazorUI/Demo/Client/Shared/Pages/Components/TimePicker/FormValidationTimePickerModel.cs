namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.TimePicker;

public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
