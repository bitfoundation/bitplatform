﻿using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class DeleteAccountSection
{
    private bool isDialogOpen;

    [AutoInject] IUserController userController = default!;

    private async Task DeleteAccount()
    {
        await AuthenticationManager.EnsurePrivilegedAccess(CurrentCancellationToken);

        await userController.Delete(CurrentCancellationToken);

        await AuthenticationManager.SignOut(deleteUserSessionFromServer: false, CurrentCancellationToken);
    }
}
