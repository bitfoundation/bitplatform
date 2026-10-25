namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ColorPicker;

public class BitColorPickerDemoFormModel
{
    [Required(ErrorMessage = "Pick a brand color")]
    public string? Color { get; set; }
}
