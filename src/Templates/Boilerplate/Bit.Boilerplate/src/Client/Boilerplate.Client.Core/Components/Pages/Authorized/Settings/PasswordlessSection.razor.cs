using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class PasswordlessSection
{
    [AutoInject] IUserController userController = default!;


    [Parameter] public EventCallback OnCredentialCreated { get; set; }


    private async Task EnablePasswordless()
    {
        var options = await userController.GetWebAuthnCredentialOptions(CurrentCancellationToken);

        var attestationResponse = await JSRuntime.CreateWebAuthnCredential(options);

        await userController.CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

        await JSRuntime.StoreWebAuthnConfigured(options.User.Name);

        await OnCredentialCreated.InvokeAsync();
    }
}
