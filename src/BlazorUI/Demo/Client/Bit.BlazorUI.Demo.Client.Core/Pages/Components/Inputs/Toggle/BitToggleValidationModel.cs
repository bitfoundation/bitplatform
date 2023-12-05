namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Toggle;

public class BitToggleValidationModel
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms and conditions.")]
    public bool TermsAgreement { get; set; } = true;
}
