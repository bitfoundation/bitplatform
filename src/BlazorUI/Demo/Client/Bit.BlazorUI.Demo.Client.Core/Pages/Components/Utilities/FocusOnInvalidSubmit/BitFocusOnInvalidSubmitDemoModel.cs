namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.FocusOnInvalidSubmit;

public class BitFocusOnInvalidSubmitDemoModel
{
    [Required(ErrorMessage = "Enter your name.")]
    public string? Name { get; set; } = "Ada";

    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter an email address.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Pick a plan.")]
    public string? Plan { get; set; }
}
