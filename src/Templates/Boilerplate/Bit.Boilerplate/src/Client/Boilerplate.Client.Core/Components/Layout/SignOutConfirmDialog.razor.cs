namespace Boilerplate.Client.Core.Components.Layout;

public partial class SignOutConfirmDialog
{
    private bool isSigningOut;


    [Parameter] public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }


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

            await AuthManager.SignOut(CurrentCancellationToken);
        }
        finally
        {
            isSigningOut = false;
        }

        await CloseModal();
    }
}
