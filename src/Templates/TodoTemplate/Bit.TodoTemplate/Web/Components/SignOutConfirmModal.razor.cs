namespace TodoTemplate.App.Components;

public partial class SignOutConfirmModal
{
    private bool isOpen;
    private bool IsOpenHasBeenSet;

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

    [Inject]
    public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    private void CloseModal()
    {
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
    }

    private async Task SignOut()
    {
        await TodoTemplateAuthenticationService.SignOut();
        CloseModal();
    }
}
