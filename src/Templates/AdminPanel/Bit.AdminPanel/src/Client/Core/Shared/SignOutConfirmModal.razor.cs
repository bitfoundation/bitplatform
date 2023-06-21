﻿namespace AdminPanel.Client.Core;

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

        await JsRuntime.SetBodyOverflow(false);
    }

    private async Task SignOut()
    {
        await AuthenticationService.SignOut();

        await CloseModal();
    }
}
