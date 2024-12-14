namespace Boilerplate.Client.Core.Components.Layout;

public partial class SignOutConfirmDialog
{
    [Parameter] public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }


    private async Task CloseModal()
    {
        IsOpen = false;
        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task SignOut()
    {
        await AuthManager.SignOut(CurrentCancellationToken);

        await CloseModal();
    }
}
