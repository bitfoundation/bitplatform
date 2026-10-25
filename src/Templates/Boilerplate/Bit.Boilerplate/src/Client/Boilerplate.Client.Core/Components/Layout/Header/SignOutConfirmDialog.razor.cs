namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class SignOutConfirmDialog
{
    private bool isSigningOut;
    private bool signOutOfAllAccounts;


    [Parameter] public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public bool CanSignOutOfAllAccounts { get; set; }


    private async Task CloseModal()
    {
        if (isSigningOut) return;

        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task SignOut()
    {
        if (isSigningOut) return;

        try
        {
            isSigningOut = true;

            if (CanSignOutOfAllAccounts && signOutOfAllAccounts)
            {
                await AuthManager.SignOutAll(CurrentCancellationToken);
            }
            else
            {
                await AuthManager.SignOut(CurrentCancellationToken);
            }
        }
        finally
        {
            isSigningOut = false;
        }

        await CloseModal();
    }

    private async Task HandleNavigation(LocationChangingContext context)
    {
        context.PreventNavigation();

        if (isSigningOut) return;

        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }
}
