using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
using Fido2NetLib;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class PasswordlessSection
{
    private bool isConfigured;


    [AutoInject] IUserController userController = default!;
    [AutoInject] IIdentityController identityController = default!;


    [Parameter] public UserDto? User { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (User?.UserName is null) return;

        isConfigured = await JSRuntime.IsWebAuthnConfigured(User.UserName);
    }


    private async Task EnablePasswordless()
    {
        if (User?.UserName is null) return;

        var options = await userController.GetWebAuthnCredentialOptions(CurrentCancellationToken);

        AuthenticatorAttestationRawResponse attestationResponse;
        try
        {
            attestationResponse = await JSRuntime.CreateWebAuthnCredential(options);
        }
        catch (Exception ex)
        {
            //
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            return;
        }

        await userController.CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

        await JSRuntime.StoreWebAuthnConfigured(User.UserName);

        isConfigured = true;

        SnackBarService.Success(Localizer[nameof(AppStrings.EnablePasswordlessSucsessMessage)]);
    }

    private async Task DisablePasswordless()
    {
        if (User?.UserName is null) return;

        var options = await identityController.GetWebAuthnAssertionOptions(CurrentCancellationToken);

        AuthenticatorAssertionRawResponse assertion;
        try
        {
            assertion = await JSRuntime.VerifyWebAuthnCredential(options);
        }
        catch (Exception ex)
        {
            //
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            return;
        }

        var verifyResult = await identityController.VerifyWebAuthAssertion(assertion, CurrentCancellationToken);

        await userController.DeleteWebAuthnCredential(assertion.Id, CurrentCancellationToken);

        await JSRuntime.RemoveWebAuthnConfigured(User.UserName);

        isConfigured = false;

        SnackBarService.Success(Localizer[nameof(AppStrings.DisablePasswordlessSucsessMessage)]);
    }
}
