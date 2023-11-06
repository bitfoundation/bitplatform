namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TimePickers.TimePicker;

public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
