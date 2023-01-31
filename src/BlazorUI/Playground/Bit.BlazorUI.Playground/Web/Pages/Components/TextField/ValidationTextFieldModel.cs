using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.TextField;

public class ValidationTextFieldModel
{
    [Required]
    public string Text { get; set; }

    [RegularExpression("0*[1-9][0-9]*", ErrorMessage = "Only numeric values are allow in field.")]
    public string NumericText { get; set; }

    [RegularExpression("^[a-zA-Z0-9.]*$", ErrorMessage = "Sorry, only letters(a-z), numbers(0-9), and periods(.) are allowed.")]
    public string CharacterText { get; set; }

    [EmailAddress(ErrorMessage = "Invalid e-mail address.")]
    public string EmailText { get; set; }

    [StringLength(maximumLength: 5, MinimumLength = 3, ErrorMessage = "The text length much be between 3 and 5 characters in length.")]
    public string RangeText { get; set; }
}
