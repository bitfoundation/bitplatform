namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}
