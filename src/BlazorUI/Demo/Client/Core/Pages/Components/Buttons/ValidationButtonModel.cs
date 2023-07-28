namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public class ValidationButtonModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonrequiredText { get; set; }
}
