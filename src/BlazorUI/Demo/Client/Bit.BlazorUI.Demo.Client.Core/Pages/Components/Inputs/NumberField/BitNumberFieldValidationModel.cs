namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumberField;

public class BitNumberFieldValidationModel
{
    [Required(ErrorMessage = "Enter an age")]
    [Range(1, 150, ErrorMessage = "Nobody is that old")]
    public int? Age { get; set; }
}
