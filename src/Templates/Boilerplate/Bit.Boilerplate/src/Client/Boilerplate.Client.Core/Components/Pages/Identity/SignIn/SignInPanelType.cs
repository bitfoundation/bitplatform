namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

/// <summary>
/// The layout type of the SignIn panel UI. Check out <see cref="SignInModalService"/> for more details
/// </summary>
public enum SignInPanelType
{
    /// <summary>
    /// Shows email, phone and password fields alongside with sign-in, send otp and sign-up buttons,
    /// suitable for standalone sign-in page.
    /// </summary>
    Full,

    /// <summary>
    /// Shows email, phone and password fields alongside with sign-in button,
    /// suitable for sign-in modal dialog.
    /// </summary>
    Password,

    /// <summary>
    /// Shows email, phone and send otp button,
    /// suitable for sign-in modal dialog.
    /// </summary>
    Otp
}
