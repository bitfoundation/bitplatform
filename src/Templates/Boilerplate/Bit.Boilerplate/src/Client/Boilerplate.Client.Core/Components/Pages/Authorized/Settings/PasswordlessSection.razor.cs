using Fido2NetLib;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

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

        isConfigured = await JSRuntime.IsWebAuthnConfigured(User.Id);
    }


    private async Task EnablePasswordless()
    {
        if (User?.UserName is null) return;

        // Only on Android this action will replace the current credential registered on the device,
        // since android won't show the user selection window when there are multiple credentials registered.
        // So it may be a good idea to show a confirm modal if this behavior is not appropriate for your app (as shown in the following commented lines):
        //var userIds = await JSRuntime.GetWebAuthnConfiguredUserIds();
        //if (userIds is not null && userIds.Length > 0)
        //{
        //    // show a warning or confirm modal
        //}

        var options = await userController.GetWebAuthnCredentialOptions(CurrentCancellationToken);

        AuthenticatorAttestationRawResponse attestationResponse;
        try
        {
            attestationResponse = await JSRuntime.CreateWebAuthnCredential(options);
        }
        catch (Exception ex)
        {
            // we can safely handle the exception thrown here since it mostly because of a timeout or user cancelling the native ui.
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            return;
        }

        await userController.CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

        await JSRuntime.SetWebAuthnConfiguredUserId(User.Id);

        isConfigured = true;

        SnackBarService.Success(Localizer[nameof(AppStrings.EnablePasswordlessSucsessMessage)]);
    }

    private async Task DisablePasswordless()
    {
        if (User?.UserName is null) return;

        var options = await identityController.GetWebAuthnAssertionOptions(new() { UserIds = [User.Id] }, CurrentCancellationToken);

        AuthenticatorAssertionRawResponse assertion;
        try
        {
            assertion = await JSRuntime.GetWebAuthnCredential(options);
        }
        catch (Exception ex)
        {
            // we can safely handle the exception thrown here since it mostly because of a timeout or user cancelling the native ui.
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            return;
        }

        var verifyResult = await identityController.VerifyWebAuthAssertion(assertion, CurrentCancellationToken);

        await userController.DeleteWebAuthnCredential(assertion.Id, CurrentCancellationToken);

        await JSRuntime.RemoveWebAuthnConfiguredUserId(User.Id);

        isConfigured = false;

        SnackBarService.Success(Localizer[nameof(AppStrings.DisablePasswordlessSucsessMessage)]);
    }

    // Only for debugging purposes, uncomment the following lines and the corresponding lines in the razor file.
    //private async Task DeleteAll()
    //{
    //    await userController.DeleteAllWebAuthnCredentials(CurrentCancellationToken);

    //    await JSRuntime.RemoveWebAuthnConfigured();

    //    isConfigured = false;
    //}
}
