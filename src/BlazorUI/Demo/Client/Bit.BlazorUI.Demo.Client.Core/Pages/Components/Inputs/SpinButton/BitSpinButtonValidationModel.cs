namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SpinButton;

public class BitSpinButtonValidationModel
{
    [Required(ErrorMessage = "Enter an age")]
    [Range(1, 150, ErrorMessage = "Nobody is that old")]
    public double AgeInYears { get; set; }
}
