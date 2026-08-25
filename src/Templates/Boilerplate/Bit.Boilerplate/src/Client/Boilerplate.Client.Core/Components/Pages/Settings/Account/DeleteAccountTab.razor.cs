namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class DeleteAccountTab
{
    private bool isWaiting;
    private bool isDialogOpen;

    [AutoInject] IUserController userController = default!;

    private async Task DeleteAccount()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken))
            {
                await userController.Delete(CurrentCancellationToken);

                await AuthManager.SignOut(CurrentCancellationToken);
            }
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }
}
