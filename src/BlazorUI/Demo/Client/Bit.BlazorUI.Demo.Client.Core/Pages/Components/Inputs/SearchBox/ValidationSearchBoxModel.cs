namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public class ValidationSearchBoxModel
{
    [StringLength(6, MinimumLength = 2,
    ErrorMessage = "Text must be between 2 and 6 chars.")]
    public string Text { get; set; } = default!;
}
