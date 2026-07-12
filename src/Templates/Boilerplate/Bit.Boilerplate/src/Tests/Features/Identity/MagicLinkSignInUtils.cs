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

    /// <summary>Types <paramref name="code"/> into the boxes of the currently visible <c>BitOtpInput</c>.</summary>
    public static async Task FillOtpInputs(IPage page, string code)
    {
        var inputs = page.Locator(".bit-otp-inp");
        await inputs.First.WaitForAsync();

        for (var i = 0; i < code.Length; i++)
        {
            await inputs.Nth(i).FillAsync(code[i].ToString());
        }
    }

    /// <summary>
    /// Signs <paramref name="email"/> in end-to-end using the magic link OTP: requests it, reads the code from the
    /// captured confirmation e-mail, types it into the OTP panel and waits for the resulting redirect to the home page.
    /// </summary>
    public static async Task SignInViaMagicLinkOtp(IPage page, AppTestServer server, string email, CancellationToken cancellationToken)
    {
        await RequestMagicLinkAndOtp(page, server.WebAppServerAddress, email);

        var (_, otpCode) = await ReadConfirmationEmail(server, email, cancellationToken);

        await FillOtpInputs(page, otpCode);

        // Filling the last digit confirms the e-mail, signs the user in and redirects to the home page.
        await page.WaitForURLAsync(server.WebAppServerAddress.ToString());
    }
}
