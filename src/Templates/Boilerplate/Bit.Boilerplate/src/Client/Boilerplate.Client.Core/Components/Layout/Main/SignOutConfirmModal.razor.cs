namespace Boilerplate.Client.Core.Components.Layout.Main;

public partial class SignOutConfirmModal
{
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
    }

    private async Task SignOut()
    {
        await AuthenticationManager.SignOut(CurrentCancellationToken);

        await CloseModal();
    }
}
