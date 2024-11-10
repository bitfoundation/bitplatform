namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public class FormValidationDropdownModel
{
    [MaxLength(2, ErrorMessage = "The property {0} have more than {1} elements")]
    [MinLength(1, ErrorMessage = "The property {0} doesn't have at least {1} elements")]
    public IEnumerable<string> Products { get; set; } = [];

    [Required]
    public string Category { get; set; } = default!;
}
