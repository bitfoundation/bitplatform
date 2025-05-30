namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInModal
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public Action? OnClose { get; set; }
    [Parameter] public Action? OnSuccess { get; set; } // The SignInModalService will show this page as a modal dialog, and this action will be invoked when the sign-in is successful.

    private void CloseModal()
    {
        OnClose?.Invoke();
    }
}
