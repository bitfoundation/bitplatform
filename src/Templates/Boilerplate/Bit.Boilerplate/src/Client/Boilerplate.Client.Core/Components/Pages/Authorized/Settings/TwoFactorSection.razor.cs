using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class TwoFactorSection
{
    private string? qrCode;
    private bool isWaiting;
    private string? sharedKey;
    private int recoveryCodesLeft;
    private bool isKeyCopiedShown;
    private bool isCodesCopiedShown;
    private string[]? recoveryCodes;
    private string? authenticatorUri;
    private string? verificationCode;
    private bool isTwoFactorAuthEnabled;


    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private IUserController userController = default!;


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
        var response = await SendTwoFactorAuthRequest(request);

        recoveryCodes = response?.RecoveryCodes;
        SnackBarService.Success(Localizer[nameof(AppStrings.TwoFactorAuthenticationEnabled)]);
    }

    private async Task DisableTwoFactorAuth()
    {
        var request = new TwoFactorAuthRequestDto { Enable = false };
        await SendTwoFactorAuthRequest(request);
        SnackBarService.Success(Localizer[nameof(AppStrings.TwoFactorAuthenticationDisabled)]);
    }

    private async Task GenerateRecoveryCode()
    {
        var request = new TwoFactorAuthRequestDto { ResetRecoveryCodes = true };
        var response = await SendTwoFactorAuthRequest(request);

        recoveryCodes = response?.RecoveryCodes;
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

    private async Task<TwoFactorAuthResponseDto?> SendTwoFactorAuthRequest(TwoFactorAuthRequestDto request)
    {
        if (isWaiting) return null;

        isWaiting = true;

        try
        {
            var response = await userController.TwoFactorAuth(request, CurrentCancellationToken);

            qrCode = response.QrCode;
            sharedKey = response.SharedKey;
            authenticatorUri = response.AuthenticatorUri;
            recoveryCodesLeft = response.RecoveryCodesLeft;
            isTwoFactorAuthEnabled = response.IsTwoFactorEnabled;

            return response;
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
            return null;
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task CopySharedKeyToClipboard()
    {
        if (isKeyCopiedShown) return;

        await clipboard.WriteText(sharedKey!);

        isKeyCopiedShown = true;
        StateHasChanged();

        await Task.Delay(1000);

        isKeyCopiedShown = false;
        StateHasChanged();
    }

    private async Task CopyRecoveryCodesToClipboard()
    {
        if (isCodesCopiedShown) return;

        await clipboard.WriteText(string.Join(Environment.NewLine, recoveryCodes ?? []));

        isCodesCopiedShown = true;
        StateHasChanged();

        await Task.Delay(1000);

        isCodesCopiedShown = false;
        StateHasChanged();
    }
}
