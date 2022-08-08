using Microsoft.JSInterop;

namespace AdminPanel.App.Components;

public partial class SignOutConfirmModal
{
    [AutoInject] private IAuthenticationService authService = default!;

    [AutoInject] private IJSRuntime jsRuntime = default!;

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

    private async Task CloseModal()
    {
        IsOpen = false;
        await jsRuntime.SetToggleBodyOverflow(false);
    }

    private async Task SignOut()
    {
        await authService.SignOut();
        CloseModal();
    }
}
