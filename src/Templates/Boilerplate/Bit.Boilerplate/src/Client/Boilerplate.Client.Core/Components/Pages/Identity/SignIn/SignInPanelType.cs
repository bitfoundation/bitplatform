namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;
public enum SignInPanelType // Checkout SignInModalService for more details
{
    Full, // Shows email,phone and password fields alongside with sign-in, send otp and sign-up buttons.
    Password, // Shows email,phone and password fields alongside with sign-in button only.
    OTP // Shows email,phone and send otp button only.
}
