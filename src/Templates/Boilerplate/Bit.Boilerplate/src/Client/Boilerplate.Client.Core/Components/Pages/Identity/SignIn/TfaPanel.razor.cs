namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class TfaPanel
{
    [Parameter] public bool IsWaiting { get; set; }

    [Parameter] public SignInRequestDto Model { get; set; } = default!;

    [Parameter] public EventCallback OnSendTfaToken { get; set; }
    [Parameter] public EventCallback OnTokenProvided { get; set; }

    private string? otpCode;
    private string? recoveryCode;

    /// <summary>
    /// The recovery code wins while it has content, otherwise the authenticator code does. This runs on every
    /// change to either control so that clearing one falls back to the other instead of submitting an empty code.
    /// </summary>
    private void SyncTwoFactorCode()
    {
        Model.TwoFactorCode = string.IsNullOrWhiteSpace(recoveryCode) ? otpCode : recoveryCode;
    }
}
