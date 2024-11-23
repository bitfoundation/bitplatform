using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class DeleteAccountSection
{
    private bool isDialogOpen;

    [AutoInject] IUserController userController = default!;
    [AutoInject] IAuthorizationService authorizationService = default!;


    private async Task DeleteAccount()
    {
        // TODO: Move the following codes to the new service
        var user = AuthTokenProvider.ParseAccessToken(await AuthTokenProvider.GetAccessToken(), validateExpiry: true);
        if (await authorizationService.AuthorizeAsync(user, AuthPolicies.PRIVILEGED_ACCESS) is { Succeeded: false } || true)
        {
            await userController.SendPrivilegedAccessToken(CurrentCancellationToken);
            // var result = await dialog service give me the code.
            // Call RefreshToken through AuthManager with the code
        }

        return;

        await userController.Delete(CurrentCancellationToken);

        await AuthenticationManager.SignOut(CurrentCancellationToken);
    }
}
