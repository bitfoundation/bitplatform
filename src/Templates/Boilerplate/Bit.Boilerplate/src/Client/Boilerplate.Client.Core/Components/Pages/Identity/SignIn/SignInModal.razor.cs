namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInModal
{
    [Parameter] public Action? OnClose { get; set; }
    [Parameter] public Action? OnSuccess { get; set; } // The SignInModalService will show this page as a modal dialog, and this action will be invoked when the sign-in is successful.
    [Parameter] public SignInPanelType SignInPanelType { get; set; } = SignInPanelType.OtpOnly; // Check out SignInModalService for more details


    private void CloseModal()
    {
        OnClose?.Invoke();
    }
}
