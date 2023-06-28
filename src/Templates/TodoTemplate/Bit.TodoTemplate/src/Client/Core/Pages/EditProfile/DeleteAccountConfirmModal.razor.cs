namespace TodoTemplate.Client.Core;

public partial class DeleteAccountConfirmModal
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    private async Task CloseModal()
    {
        IsOpen = false;

        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task DeleteAccount()
    {
        await HttpClient.DeleteAsync("User/Delete");

        await AuthenticationService.SignOut();

        await CloseModal();
    }
}
