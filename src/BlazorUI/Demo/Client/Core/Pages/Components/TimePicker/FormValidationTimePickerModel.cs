namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.TimePicker;

public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}
