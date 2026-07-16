using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Helpers for the magic link / one-time-password (OTP) sign-in flow, shared by the identity and the
/// multi-tenant UI tests.
/// <para>
/// Instead of delivering e-mails, the test server captures every outgoing message in-memory as it is requested (See
/// <see cref="TestIdentityEmailService"/>), which lets a test read back the confirmation link / OTP that would otherwise
/// only land in the user's mailbox. Each test owns its own server instance (so its captures are already isolated), and
/// every lookup here is additionally scoped by the account's e-mail address so it can never pick up another account's message.
/// </para>
/// </summary>
public static class MagicLinkSignInUtils
{
    /// <summary>A brand-new, unique e-mail address; its local part is a Guid that isolates this account's captured e-mails.</summary>
    public static string NewTestEmail() => $"{Guid.NewGuid()}@bitplatform.dev";

    /// <summary>
    /// Requests a magic link / OTP for <paramref name="email"/> through the Sign in page. A brand-new e-mail makes the
    /// server register the (still unconfirmed) account, e-mail the confirmation link + OTP and show the OTP panel.
    /// </summary>
    public static async Task RequestMagicLinkAndOtp(IPage page, Uri serverAddress, string email)
    {
        await page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await RequestMagicLinkAndOtpOnCurrentPanel(page, email);
    }

    /// <summary>
    /// Requests a magic link / OTP for <paramref name="email"/> through the <c>SignInPanel</c> that is already visible on
    /// the current page - e.g. the <c>SignInModal</c> that <c>SignInModalService</c> pops up over a public page such as
    /// the product page - without navigating to the Sign in page first. A brand-new e-mail makes the server register the
    /// (still unconfirmed) account, e-mail the confirmation link + OTP and reveal the OTP panel.
    /// </summary>
    public static async Task RequestMagicLinkAndOtpOnCurrentPanel(IPage page, string email)
    {
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);

        // The button stays disabled until the debounced e-mail value is committed, so Playwright waits for it to enable.
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendMagicLinkButtonText }).ClickAsync();

        // The OTP panel (its 6 input boxes) shows up once the confirmation e-mail has been sent for the new account.
        await page.Locator(".bit-otp-inp").First.WaitForAsync();
    }

    /// <summary>
    /// Reads the confirmation e-mail the server sent to <paramref name="email"/> and returns both the magic link (its
    /// <see cref="CapturedEmail.Link"/>, rebuilt against the server's address) and the 6 digit OTP (its
    /// <see cref="CapturedEmail.Token"/>, the same code the confirm link carries as its <c>emailToken</c>).
    /// </summary>
    public static async Task<(string ConfirmUrl, string OtpCode)> ReadConfirmationEmail(
        AppTestServer server, string email, CancellationToken cancellationToken)
    {
        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.EmailToken, cancellationToken);

        var confirmUrl = new Uri(server.WebAppServerAddress, captured.Link!.PathAndQuery).ToString();
        return (confirmUrl, captured.Token!);
    }

    /// <summary>
    /// Signs <paramref name="email"/> in end-to-end using the magic link OTP: requests it, reads the code from the
    /// captured confirmation e-mail, types it into the OTP panel and waits for the resulting redirect to the home page.
    /// </summary>
    public static async Task SignInViaMagicLinkOtp(IPage page, AppTestServer server, string email, CancellationToken cancellationToken)
    {
        await RequestMagicLinkAndOtp(page, server.WebAppServerAddress, email);

        var (_, otpCode) = await ReadConfirmationEmail(server, email, cancellationToken);

        await BitOtpInputUtils.FillOtpInputs(page, otpCode);

        // Filling the last digit confirms the e-mail, signs the user in and redirects to the home page.
        await page.WaitForURLAsync(server.WebAppServerAddress.ToString());
    }

    /// <summary>
    /// Signs an <b>already confirmed</b> account in again through the magic link OTP flow - i.e. any sign-in after the
    /// very first one. A repeat sign-in differs from <see cref="SignInViaMagicLinkOtp"/> only in the e-mail the server
    /// sends: since the account is already confirmed there is nothing left to confirm, so the code arrives as a plain
    /// OTP (<see cref="CapturedEmailKind.Otp"/>) rather than the confirmation code (<see cref="CapturedEmailKind.EmailToken"/>)
    /// a brand-new account receives. Each call creates a brand-new <c>UserSession</c> for the account.
    /// </summary>
    public static async Task SignInAgainViaMagicLinkOtp(IPage page, AppTestServer server, string email, CancellationToken cancellationToken)
    {
        await RequestMagicLinkAndOtp(page, server.WebAppServerAddress, email);

        // Waiting for the OTP panel above guarantees SendOtp has already finished (and captured this e-mail), so the
        // newest captured OTP is the code this sign-in just triggered, not a leftover one from an earlier sign-in.
        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, cancellationToken);

        await BitOtpInputUtils.FillOtpInputs(page, captured.Token!);

        // Filling the last digit signs the account in and redirects to the home page.
        await page.WaitForURLAsync(server.WebAppServerAddress.ToString());
    }
}
