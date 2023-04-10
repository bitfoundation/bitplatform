namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.OtpInput;

public class ValidationOtpInputModel
{
    [Required(ErrorMessage = "Is required.")]
    [MinLength(6, ErrorMessage = "Minimum length is 6.")]
    public string OtpValue { get; set; }
}
