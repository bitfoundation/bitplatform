namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TextField;

public class ValidationTextFieldModel
{
    [Required(ErrorMessage = "This field is required.")]
    public string Text { get; set; } = default!;

    [RegularExpression("0*[1-9][0-9]*", ErrorMessage = "Only numeric values are allowed.")]
    public string NumericText { get; set; } = default!;

    [RegularExpression("^[a-zA-Z0-9.]*$", ErrorMessage = "Only letters(a-z), numbers(0-9), and period(.) are allowed.")]
    public string CharacterText { get; set; } = default!;

    [EmailAddress(ErrorMessage = "Invalid e-mail address.")]
    public string EmailText { get; set; } = default!;

    [StringLength(5, MinimumLength = 3, ErrorMessage = "The text length must be between 3 and 5 chars.")]
    public string RangeText { get; set; } = default!;
}
