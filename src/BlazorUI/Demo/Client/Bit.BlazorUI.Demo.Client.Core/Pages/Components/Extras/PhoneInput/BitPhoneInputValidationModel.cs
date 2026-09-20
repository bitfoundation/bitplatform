using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PhoneInput;

public class BitPhoneInputValidationModel
{
    [Required(ErrorMessage = "Enter a phone number.")]
    [RegularExpression(@"^\+[0-9]{8,15}$", ErrorMessage = "Enter a valid international phone number.")]
    public string? Phone { get; set; }
}
