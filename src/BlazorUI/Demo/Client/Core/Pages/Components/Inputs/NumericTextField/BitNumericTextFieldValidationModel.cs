namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumericTextField;

public class BitNumericTextFieldValidationModel
{
    [Required(ErrorMessage = "Enter an age")]
    [Range(1, 150, ErrorMessage = "Nobody is that old")]
    public double AgeInYears { get; set; }
}
