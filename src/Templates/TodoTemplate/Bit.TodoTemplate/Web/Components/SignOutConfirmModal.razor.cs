namespace TodoTemplate.App.Components;

public partial class SignOutConfirmModal
{
    [AutoInject] public ITodoTemplateAuthenticationService todoTemplateAuthenticationService = default!;

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
        await todoTemplateAuthenticationService.SignOut();
        CloseModal();
    }
}
