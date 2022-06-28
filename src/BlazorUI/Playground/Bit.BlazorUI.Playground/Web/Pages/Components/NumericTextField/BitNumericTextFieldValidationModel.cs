using System.ComponentModel.DataAnnotations;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.NumericTextField
{
    public class BitNumericTextFieldValidationModel
    {
        [Required(ErrorMessage = "Enter an age")]
        [Range(1, 200, ErrorMessage = "Nobody is that old")]
        public double AgeInYears { get; set; }
    }
}
