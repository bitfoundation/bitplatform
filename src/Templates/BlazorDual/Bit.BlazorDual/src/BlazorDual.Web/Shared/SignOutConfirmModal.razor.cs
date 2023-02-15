namespace BlazorDual.Web;

public partial class SignOutConfirmModal
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    private async Task CloseModal()
    {
        IsOpen = false;

        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task SignOut()
    {
        await AuthenticationService.SignOut();

        await CloseModal();
    }
}
