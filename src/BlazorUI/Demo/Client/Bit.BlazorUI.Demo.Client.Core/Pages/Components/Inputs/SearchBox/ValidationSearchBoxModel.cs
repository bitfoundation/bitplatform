namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2,
    ErrorMessage = "The text field length must be between 6 and 2 characters in length.")]
    public string Text { get; set; } = default!;
}
