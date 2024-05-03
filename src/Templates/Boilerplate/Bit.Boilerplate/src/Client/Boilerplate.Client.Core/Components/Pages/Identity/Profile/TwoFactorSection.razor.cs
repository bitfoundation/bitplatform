using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class TwoFactorSection
{
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private Clipboard clipboard = default!;


    private string? qrCode;
    private bool isLoading;
    private string? sharedKey;
    private int recoveryCodesLeft;
    private string[]? recoveryCodes;
    private string? authenticatorUri;
    private string? verificationCode;
    private bool isTwoFactorAuthEnabled;


    private string? message;
    private BitMessageBarType messageType;


    protected override async Task OnInitAsync()
    {
        await SendTwoFactorAuthRequest(new());

        await base.OnInitAsync();
    }

    private async Task EnableTwoFactorAuth()
    {
        if (string.IsNullOrWhiteSpace(verificationCode)) return;

        // Strip spaces and hyphens
        var twoFactorCode = verificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var request = new TwoFactorAuthRequestDto { Enable = true, TwoFactorCode = twoFactorCode };
        await SendTwoFactorAuthRequest(request);
    }

    private async Task DisableTwoFactorAuth()
    {
        var request = new TwoFactorAuthRequestDto { Enable = false };
        await SendTwoFactorAuthRequest(request);
    }

    private async Task GenerateRecoveryCode()
    {
        var request = new TwoFactorAuthRequestDto { ResetRecoveryCodes = true };
        await SendTwoFactorAuthRequest(request);
    }

    private async Task ResetAuthenticatorKey()
    {
        var request = new TwoFactorAuthRequestDto { ResetSharedKey = true };
        await SendTwoFactorAuthRequest(request);
    }

    //private async Task ForgetMachine()
    //{
    //    var request = new TwoFactorAuthRequestDto { ForgetMachine = true };
    //    await SendTwoFactorAuthRequest(request);
    //}

    private async Task SendTwoFactorAuthRequest(TwoFactorAuthRequestDto request)
    {
        isLoading = true;

        try
        {
            var response = await identityController.TwoFactorAuth(request);

            qrCode = response.QrCode;
            sharedKey = response.SharedKey;
            authenticatorUri = response.AuthenticatorUri;
            recoveryCodesLeft = response.RecoveryCodesLeft;
            isTwoFactorAuthEnabled = response.IsTwoFactorEnabled;
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageType = BitMessageBarType.Error;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CopyToClipboard()
    {
        await clipboard.WriteText(sharedKey!);
    }
}
