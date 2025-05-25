namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public enum SignInPanelType // Check out SignInModalService for more details
{
    Full, // Shows email,phone and password fields alongside with sign-in, send otp and sign-up buttons.
    PasswordOnly, // Shows email,phone and password fields alongside with sign-in button only.
    OtpOnly // Shows email,phone and send otp button only.
}
