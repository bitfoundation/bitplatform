using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class PasswordlessSection
{
    private bool isConfigured;


    [AutoInject] IUserController userController = default!;


    private async Task EnablePasswordless()
    {
        var options = await userController.GetWebAuthnCredentialOptions(CurrentCancellationToken);

        var attestationResponse = await JSRuntime.CreateWebAuthnCredential(options);

        await userController.CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

        await JSRuntime.StoreWebAuthnConfigured(options.User.Name);

        isConfigured = true;
    }

    private async Task DisablePasswordless()
    {
        var options = await userController.GetWebAuthnCredentialOptions(CurrentCancellationToken);

        await JSRuntime.RemoveWebAuthnConfigured(options.User.Name);

        isConfigured = false;
    }
}
