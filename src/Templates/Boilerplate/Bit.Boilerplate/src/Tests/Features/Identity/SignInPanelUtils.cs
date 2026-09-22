namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Types credentials into the <c>SignInPanel</c> - the sign in page's, or the <c>SignInModal</c>'s - in a way that
/// survives pre-rendering.
/// <para>
/// With pre-rendering on, the panel is on screen well before the app is interactive, and everything in it is markup
/// nobody is listening to: a value typed into it is discarded the moment hydration swaps that subtree out, and the
/// test goes on to click a button whose model never received anything. The symptom is never "the fill failed" - it is
/// the Continue click bouncing back to the sign in page with "Either provide username, email or phone number", or the
/// send button staying disabled until the test times out on it.
/// </para>
/// </summary>
public static class SignInPanelUtils
{
    /// <summary>
    /// The panel's send button, which <c>SignInPanel</c> enables on <c>model.Email is not null</c> - so it becoming
    /// enabled is proof that the app's C#, and not merely the DOM, is holding the address.
    /// </summary>
    public static ILocator EmailAcceptedSignal(IPage page) =>
        page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendMagicLinkButtonText });

    /// <summary>
    /// Fills the e-mail field and does not return until the panel's model has the address. Only for the sign in panel:
    /// other forms share nothing with it but the placeholder, and there the signal below never resolves.
    /// </summary>
    public static async Task FillEmail(IPage page, string email)
    {
        // Wait for the app to attach before touching anything: until then the panel is markup nobody is listening to,
        // and on a cold WebAssembly boot that lasts far longer than any fill loop can sensibly keep retrying for. Doing
        // it here rather than inside the loop is also what keeps the signal below cheap - by then the button is live,
        // so asking whether it is enabled answers at once instead of waiting for it to exist.
        await page.WaitForBlazorInteractive();

        var emailAccepted = EmailAcceptedSignal(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder)
                  .FillEnsuringStable(email, until: () => emailAccepted.IsEnabledAsync());
    }

    /// <summary>
    /// Fills both credential fields. The e-mail goes first and carries the wait, so by the time the password is typed
    /// the panel is known to be interactive - the password field has no signal of its own, since nothing on the panel
    /// is gated on it.
    /// </summary>
    public static async Task FillCredentials(IPage page, string email, string password)
    {
        await FillEmail(page, email);

        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);
    }
}
