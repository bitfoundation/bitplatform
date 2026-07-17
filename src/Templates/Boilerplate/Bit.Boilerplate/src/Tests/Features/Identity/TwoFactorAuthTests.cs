using OtpNet;
using System.Text.RegularExpressions;
using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class TwoFactorAuthTests : AppPageTest
{
    /// <summary>
    /// Full authenticator-app (TOTP) two-factor journey for a brand-new user:
    /// <list type="number">
    /// <item>She signs in for the first time with a magic link OTP (the same flow as <see cref="MagicLinkSignInTests"/>) - no 2FA yet.</item>
    /// <item>On the settings page she configures an authenticator app: the test reads the shared secret straight from the
    /// QR's <c>otpauth://</c> link and, playing the role of the authenticator app via <c>Otp.NET</c>, computes the
    /// verification code to enable 2FA.</item>
    /// <item>Enabling 2FA regenerates the security stamp, so a warning snack bar tells her she'll be signed out of all her
    /// devices soon - the test asserts that warning shows.</item>
    /// <item>She signs out, then signs in again with a fresh magic link OTP; this time, because 2FA is on, the OTP alone
    /// isn't enough and the 2-factor panel asks for the authenticator code, which the test computes and passes.</item>
    /// <item>Signing in with 2FA elevates her session from the very first moment (See <see cref="AuthPolicies.ELEVATED_ACCESS"/>),
    /// so deleting her account - a harmful, elevation-gated operation - needs no extra elevated-access token: no such
    /// e-mail is sent and no elevated-access OTP prompt appears; the account is deleted and she's signed straight out.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task User_Should_Enable2fa_SignInWith2fa_AndDeleteAccountFromElevatedSession()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);
        var serverAddress = server.WebAppServerAddress;

        var email = MagicLinkSignInUtils.NewTestEmail();

        // 1. First sign-in with the magic link OTP registers and signs in the brand-new account (no 2FA configured yet).
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);

        // 2. Configure the authenticator app and enable 2FA using a TOTP code we compute from its shared secret.
        var authenticatorSecret = await StartAuthenticatorSetup(Page, serverAddress);
        await EnableTwoFactor(Page, authenticatorSecret);

        // 3. Enabling 2FA succeeds and warns her she'll be signed out of all devices within a few minutes.
        await Expect(Page.GetByText(AppStrings.TwoFactorAuthenticationEnabled)).ToBeVisibleAsync();
        await Expect(Page.GetByText(AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();

        // 4. She signs out.
        await SignOut(Page);

        // 5. She signs in again; with 2FA on, the magic link OTP is only the first step - she must also pass the authenticator code.
        await SignInPassingTwoFactor(Page, server, email, authenticatorSecret);

        // 6. Her 2FA sign-in made the session elevated from the start, so she can delete her account without a fresh
        //    elevated-access token / OTP prompt.
        await DeleteAccountFromElevatedSession(Page, server, serverAddress, email);
    }

    /// <summary>
    /// Opens the 2FA section of the settings page (where the authenticator setup shows automatically for a user without
    /// 2FA) and returns the Base32 shared secret read from the QR's <c>otpauth://</c> link - the same key an authenticator
    /// app would import, which we then use to generate TOTP codes.
    /// </summary>
    private async Task<string> StartAuthenticatorSetup(IPage page, Uri serverAddress)
    {
        await page.GotoAsync(new Uri(serverAddress, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Tfa}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The QR image links to an otpauth:// URI whose "secret" query parameter is the raw (unformatted) authenticator key.
        var qrLink = page.Locator("a[href^='otpauth']");
        await qrLink.First.WaitForAsync();
        var authenticatorUri = await qrLink.First.GetAttributeAsync("href")
            ?? throw new InvalidOperationException("The authenticator setup (otpauth) link was not found.");

        var secret = Regex.Match(authenticatorUri, "secret=([^&]+)").Groups[1].Value;
        Assert.IsFalse(string.IsNullOrEmpty(secret), "Failed to read the authenticator shared secret from the QR link.");
        return secret;
    }

    /// <summary>Fills the verification code (a TOTP computed from <paramref name="authenticatorSecret"/>) and enables 2FA.</summary>
    private async Task EnableTwoFactor(IPage page, string authenticatorSecret)
    {
        var verificationCode = page.GetByPlaceholder(AppStrings.TfaConfigureAutAppVerificationCodePlaceholder);
        await verificationCode.FillAsync(ComputeTotpCode(authenticatorSecret));
        // The field commits its value on change (blur), so blur it before verifying to make sure the component reads it.
        await verificationCode.BlurAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaConfigureAutAppVerifyButtonText }).ClickAsync();
    }

    /// <summary>Signs the current user out through the header menu and its confirmation dialog.</summary>
    private async Task SignOut(IPage page)
    {
        // Open the user menu in the header (clicking its persona) then click its "Sign out" action.
        await page.Locator(".bit-prs.persona").First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();

        // Confirm in the dialog (its OK button is also labelled "Sign out"; the menu one is gone once the dialog is up).
        await Expect(page.GetByText(AppStrings.SignOutPrompt)).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).Last.ClickAsync();

        // Signed out: the (authorize-only) settings page she was on now renders the "not authorized" page.
        await Expect(page).ToHaveTitleAsync(AppStrings.NotAuthorizedPageTitle);
    }

    /// <summary>
    /// Signs the (now existing, 2FA-enabled) account in with a magic link OTP and then satisfies the resulting two-factor
    /// challenge with a TOTP code computed from <paramref name="authenticatorSecret"/>.
    /// </summary>
    private async Task SignInPassingTwoFactor(IPage page, AppTestServer server, string email, string authenticatorSecret)
    {
        // Ask for a magic link / OTP for the existing account and type the 6 digits from the captured OTP e-mail.
        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(page, server.WebAppServerAddress, email);

        var otpEmail = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);
        await BitOtpInputUtils.FillOtpInputs(page, otpEmail.Token!);

        // Because 2FA is enabled, the OTP only clears the first step; the 2-factor panel now asks for the authenticator code.
        await Expect(page.GetByText(AppStrings.TfaPanelTitle)).ToBeVisibleAsync();
        await BitOtpInputUtils.FillOtpInputs(page, ComputeTotpCode(authenticatorSecret));

        // Passing 2FA finishes the sign-in and redirects her to the home page as herself.
        await page.WaitForURLAsync(server.WebAppServerAddress.ToString());
        await Expect(page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
    }

    /// <summary>
    /// Deletes the account from the settings page and asserts it happened without any elevated-access step: no
    /// elevated-access token e-mail is sent (the 2FA sign-in already elevated the session) and the deletion signs her out.
    /// </summary>
    private async Task DeleteAccountFromElevatedSession(IPage page, AppTestServer server, Uri serverAddress, string email)
    {
        await page.GotoAsync(new Uri(serverAddress, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Switch to the account section's "Delete" tab, then start (and confirm) the account deletion.
        await page.GetByText(AppStrings.Delete, new() { Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.DeleteAccount }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Yes }).ClickAsync();

        // With an already-elevated session, deletion needs no elevated-access token, so the elevated-access OTP prompt
        // never appears; the account is deleted and she is signed out - the settings page becomes "not authorized".
        await Expect(page).ToHaveTitleAsync(AppStrings.NotAuthorizedPageTitle);

        // No elevated-access token e-mail was sent: the 2FA sign-in already elevated the session. Read every e-mail the
        // server captured straight from its in-memory store (See TestIdentityEmailService / EmailCaptureStore).
        var capturedEmails = server.WebApp.Services.GetRequiredService<EmailCaptureStore>().Captured;
        Assert.IsFalse(
            capturedEmails.Any(capturedEmail => capturedEmail.IsTo(email) && capturedEmail.Kind is CapturedEmailKind.ElevatedAccess),
            "The 2FA sign-in already elevated the session, so deleting the account must not send an elevated-access token e-mail.");
    }

    /// <summary>
    /// Computes the current TOTP code for a Base32 <paramref name="base32Secret"/>. ASP.NET Core Identity's authenticator
    /// uses standard TOTP (HMAC-SHA1, 6 digits, 30s time-step), which matches <c>Otp.NET</c>'s defaults.
    /// </summary>
    private static string ComputeTotpCode(string base32Secret)
    {
        var totp = new Totp(Base32Encoding.ToBytes(base32Secret));
        return totp.ComputeTotp();
    }
}
