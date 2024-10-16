using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Main.Settings;

public partial class DeleteAccountSection
{
    private bool isDialogOpen;


    [AutoInject] IUserController userController = default!;


    private async Task DeleteAccount()
    {
        await userController.Delete(CurrentCancellationToken);

        await AuthenticationManager.SignOut(CurrentCancellationToken);
    }
}
