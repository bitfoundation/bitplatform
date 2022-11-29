using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.SpinButton;

public class BitSpinButtonValidationModel
{
    [Required(ErrorMessage = "Enter an age")]
    [Range(1, 200, ErrorMessage = "Nobody is that old")]
    public double AgeInYears { get; set; }
}
