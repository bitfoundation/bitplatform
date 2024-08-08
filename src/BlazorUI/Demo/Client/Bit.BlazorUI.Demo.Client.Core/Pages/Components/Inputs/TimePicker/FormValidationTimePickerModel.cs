namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TimePicker;

public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
