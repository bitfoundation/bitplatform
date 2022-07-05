namespace AdminPanelTemplate.App.Components;

public partial class SignOutConfirmModal
{
    [AutoInject] private IAuthenticationService authService = default!;

    private bool isOpen;

    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (value == isOpen) return;
            isOpen = value;
            _ = IsOpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    private void CloseModal()
    {
        IsOpen = false;
    }

    private async Task SignOut()
    {
        await authService.SignOut();
        CloseModal();
    }
}
