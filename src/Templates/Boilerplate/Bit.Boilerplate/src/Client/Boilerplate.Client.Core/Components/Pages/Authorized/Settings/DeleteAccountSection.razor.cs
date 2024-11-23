using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class DeleteAccountSection
{
    private bool isDialogOpen;

    [AutoInject] IUserController userController = default!;
    [AutoInject] IAuthorizationService authorizationService = default!;


    private async Task DeleteAccount()
    {
        // TODO: Move the following codes to the new service + localization
        var user = AuthTokenProvider.ParseAccessToken(await AuthTokenProvider.GetAccessToken(), validateExpiry: true);
        if (await authorizationService.AuthorizeAsync(user, AuthPolicies.PRIVILEGED_ACCESS) is { Succeeded: false } || true)
        {
            try
            {
                await userController.SendPrivilegedAccessToken(CurrentCancellationToken);
            }
            catch (TooManyRequestsExceptions exp)
            {
                ExceptionHandler.Handle(exp);
            }
            var token = await JSRuntime.InvokeAsync<string?>("prompt", "Please enter the privileged access token to continue.");
            await AuthenticationManager.RefreshToken("RequestPrivilegedAccess", token);
        }

        await userController.Delete(CurrentCancellationToken);

        await AuthenticationManager.SignOut(CurrentCancellationToken);
    }
}
