using System.Text;
using System.Text.Encodings.Web;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class TwoFactorSection
{
    private UserDto? user;

    [AutoInject] private IIdentityController identityController = default!;


    private bool isLoading;
    private bool isEnabling;
    private string? sharedKey;
    private int recoveryCodesLeft;
    private string[]? recoveryCodes;
    private bool isMachineRemembered;
    private string? authenticatorUri;
    private string? verificationCode;
    private bool isTwoFactorAuthEnabled;


    private string? message;
    private BitMessageBarType messageType;


    protected override async Task OnInitAsync()
    {
        isLoading = true;

        try
        {
            await LoadTwoFactorAuthData();
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

        await base.OnInitAsync();
    }


    private async Task LoadTwoFactorAuthData()
    {
        var request = new TwoFactorAuthRequestDto { };
        var response = await identityController.TwoFactorAuth(request);

        sharedKey = response.SharedKey;
        authenticatorUri = response.AuthenticatorUri;
        isTwoFactorAuthEnabled = response.IsTwoFactorEnabled;
        recoveryCodesLeft = response.RecoveryCodesLeft;
        isMachineRemembered = response.IsMachineRemembered;
    }

    private async Task EnableTwoFactorAuth()
    {
        if (isEnabling) return;

        message = null;
        isEnabling = true;

        try
        {
            if (string.IsNullOrWhiteSpace(verificationCode)) return;

            // Strip spaces and hyphens
            var strippedVerificationCode = verificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var request = new TwoFactorAuthRequestDto { Enable = true, TwoFactorCode = strippedVerificationCode };
            var response = await identityController.TwoFactorAuth(request, CurrentCancellationToken);

            isTwoFactorAuthEnabled = true;

            messageType = BitMessageBarType.Success;
            message = Localizer[nameof(AppStrings.TfaAuthenticatorAppVerifiedMessage)];

            if (response.RecoveryCodesLeft == 0)
            {
                recoveryCodes = response.RecoveryCodes;
            }
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageType = BitMessageBarType.Error;
        }
        finally
        {
            isEnabling = false;
        }
    }

    private async Task ForgetMachine()
    {
        var request = new TwoFactorAuthRequestDto { ForgetMachine = true };
        await identityController.TwoFactorAuth(request, CurrentCancellationToken);
    }

    private async Task DisableTwoFactorAuth()
    {
        var request = new TwoFactorAuthRequestDto { Enable = false };
        await identityController.TwoFactorAuth(request, CurrentCancellationToken);

        NavigationManager.Refresh(true);
    }

    private async Task GenerateRecoveryCode()
    {
        var request = new TwoFactorAuthRequestDto { ResetRecoveryCodes = true };
        var response = await identityController.TwoFactorAuth(request, CurrentCancellationToken);

        recoveryCodes = response.RecoveryCodes;
    }

    private async Task ResetAuthenticatorKey()
    {
        var request = new TwoFactorAuthRequestDto { ResetSharedKey = true };
        await identityController.TwoFactorAuth(request, CurrentCancellationToken);

        NavigationManager.Refresh(true);
    }
}
