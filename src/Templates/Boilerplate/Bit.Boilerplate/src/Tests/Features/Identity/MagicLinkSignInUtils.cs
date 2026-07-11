using System.Text.RegularExpressions;
using Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Helpers for the magic link / one-time-password (OTP) sign-in flow, shared by the identity and the
/// multi-tenancy UI tests.
/// <para>
/// In the Development environment every outgoing e-mail is also written to the process-wide
/// <see cref="DiagnosticLogger.Store"/> (See <c>IdentityEmailService.LogSendEmail</c>), which lets a test read back
/// the confirmation link / OTP that would otherwise only land in the user's mailbox. Because that store is a static
/// queue shared by every test running in parallel, every lookup here is scoped by the account's unique random e-mail
/// address so it can never pick up another test's message.
/// </para>
/// </summary>
public static class MagicLinkSignInUtils
{
    /// <summary>A brand-new, unique e-mail address; its local part is a Guid that isolates this account's logged e-mails.</summary>
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
    /// Reads the confirmation e-mail the dev environment logged for <paramref name="email"/> and returns both the magic
    /// link (rebuilt against the server's address) and the 6 digit OTP embedded in it as its <c>emailToken</c>.
    /// </summary>
    public static async Task<(string ConfirmUrl, string OtpCode)> ReadConfirmationEmailFromDiagnosticLog(
        AppTestServer server, string email, CancellationToken cancellationToken)
    {
        // The e-mail's delivery runs as a Hangfire background job that the server enqueues right after logging the
        // e-mail's contents, so waiting for that job to drain guarantees the confirmation e-mail is already logged.
        await server.WaitForBackgroundJobsToComplete(cancellationToken);

        var uniqueKey = email.Split('@')[0]; // the Guid local part uniquely identifies this account's e-mails.

        // The magic link points at the Confirm page and carries the emailToken (the same 6 digit code shown in the e-mail).
        var linkRegex = new Regex(
            $@"https?://[^\s""'<>]*{Regex.Escape(PageUrls.Confirm)}\?[^\s""'<>]*emailToken=(?<code>[^\s""'<>&]+)[^\s""'<>]*",
            RegexOptions.IgnoreCase);

        foreach (var log in DiagnosticLogger.Store)
        {
            var message = log.Message;
            if (message is null || message.Contains(uniqueKey, StringComparison.OrdinalIgnoreCase) is false)
                continue;

            var match = linkRegex.Match(message);
            // Make sure the matched link really is this account's (the store may hold other accounts' links too).
            if (match.Success is false || match.Value.Contains(uniqueKey, StringComparison.OrdinalIgnoreCase) is false)
                continue;

            var confirmUrl = new Uri(server.WebAppServerAddress, new Uri(match.Value).PathAndQuery).ToString();
            return (confirmUrl, Uri.UnescapeDataString(match.Groups["code"].Value));
        }

        throw new InvalidOperationException($"No confirmation e-mail (magic link) was found in the diagnostic log for '{email}'.");
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
    /// logged confirmation e-mail, types it into the OTP panel and waits for the resulting redirect to the home page.
    /// </summary>
    public static async Task SignInViaMagicLinkOtp(IPage page, AppTestServer server, string email, CancellationToken cancellationToken)
    {
        await RequestMagicLinkAndOtp(page, server.WebAppServerAddress, email);

        var (_, otpCode) = await ReadConfirmationEmailFromDiagnosticLog(server, email, cancellationToken);

        await FillOtpInputs(page, otpCode);

        // Filling the last digit confirms the e-mail, signs the user in and redirects to the home page.
        await page.WaitForURLAsync(server.WebAppServerAddress.ToString());
    }
}
