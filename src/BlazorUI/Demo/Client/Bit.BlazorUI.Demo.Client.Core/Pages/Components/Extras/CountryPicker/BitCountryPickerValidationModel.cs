using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CountryPicker;

public class BitCountryPickerValidationModel
{
    [Required(ErrorMessage = "Select your nationality.")]
    public BitCountry? Nationality { get; set; }
}
