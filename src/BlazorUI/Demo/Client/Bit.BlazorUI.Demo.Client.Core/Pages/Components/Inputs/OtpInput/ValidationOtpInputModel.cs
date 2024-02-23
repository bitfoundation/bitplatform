namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.OtpInput;

public class ValidationOtpInputModel
{
    [Required(ErrorMessage = "The OTP value is required.")]
    [MinLength(6, ErrorMessage = "Minimum length is 6.")]
    public string OtpValue { get; set; } = default!;
}
