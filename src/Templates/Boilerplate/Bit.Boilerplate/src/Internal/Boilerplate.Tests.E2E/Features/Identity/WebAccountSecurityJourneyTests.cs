using OtpNet;
using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>
/// Everything an account owner can change about how she gets back in, on the live AdminPanel and through the app's own
/// pages. Four journeys, each on a brand-new account of its own:
/// <list type="number">
/// <item><b>The password</b>: setting one by resetting it, changing it afterwards, and signing in with each - including
/// the half a success message cannot show, that the password she replaced stops working.</item>
/// <item><b>The identifiers</b>: moving the account onto a phone number and then onto another e-mail address, and
/// signing in through each of them afterwards - the texted one-time-password, and the magic link.</item>
/// <item><b>The second factor</b>: enrolling an authenticator app, signing in through it, and turning it off.</item>
/// <item><b>The passkey</b>: enrolling one, signing in with it, and deleting it again.</item>
/// </list>
/// <para>
/// Every code these flows turn on is read out of the deployment's own Hangfire jobs through <c>/dev-mcp</c> (See
/// <see cref="AppTestBase.WaitForSixDigit"/>) - the mails it rendered and the messages it texted, exactly as they were
/// sent. Nothing here knows a token the user would not have had, and nothing is faked: the second factor's codes are
/// computed from the shared key the QR hands out, and the passkey ceremonies run against a CDP virtual authenticator.
/// </para>
/// <para>
/// The phone numbers are in the +1 555-01xx range that North America reserves for fiction, so the text messages the
/// deployment really does send (its Twilio account is live) can never reach anybody. Twilio rejects them as
/// unroutable, which costs nothing and leaves no job behind - <c>PhoneServiceJobsRunner</c> deletes its own after
/// three attempts - and the code is read from the job's arguments, which are written when it is enqueued.
/// </para>
/// <para>
/// AdminPanel rather than Sales: identity here runs on the standalone AdminPanelApi, whose Hangfire the Dev MCP can
/// read. Sales keeps its jobs in an isolated SQLite of its own, so nothing sent from there is readable at all.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebAccountSecurityJourneyTests : AppTestBase
{
    private const string firstPassword = "e2e-first-password";
    private const string secondPassword = "e2e-second-password";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// <list type="number">
    /// <item>A brand-new account, created and confirmed by the sign-in page itself - which leaves it with no password
    /// its owner knows (See <see cref="AppTestBase.SignInNewUser"/>), so "forgot password" is how she gets her first
    /// one: from the sign-in page's link she asks for a code, types the one the mail carries and sets it.</item>
    /// <item>That password signs her in.</item>
    /// <item>On Settings &gt; Account &gt; Password she changes it, quoting the old one. The app says so and warns her
    /// that every device is about to be signed out - the security stamp has rotated.</item>
    /// <item>She signs out: the password she replaced is refused, and the new one lets her in.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task User_Should_ResetHerPassword_ChangeIt_AndSignInWithEachOne()
    {
        await SkipWithoutGlobalAdminCredentials();

        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken)).McpClient!;
        var email = NewTestEmail();

        RegisterForCleanup(() => DeleteAccounts([email], []));

        var page = await OpenApp(App.AdminPanel);
        await SignInNewUser(page, email, firstPassword, mcp);

        // 1. Her first real password is the one she sets by resetting it: the account was auto-provisioned with a
        //    random one, and what she typed on the sign-in page above was never stored.
        await SignOut(page);
        await ResetPassword(page, mcp, email, firstPassword);
        await SignInWithPassword(page, email, firstPassword);

        // 2. Change it from the settings page.
        await page.GoToInApp(AccountSectionUrl);
        // Passwordless is the pivot's first (and so default) tab, so the password form has to be asked for. Exact,
        // because "Password" is a prefix of that first tab's own label.
        await page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Password, Exact = true }).ClickAsync();

        // By label rather than placeholder - this form has none - and exactly, since "New Password" is a substring of
        // "Confirm New Password".
        await page.GetByLabel(AppStrings.OldPassword, new() { Exact = true }).FillEnsuringStable(firstPassword);
        await page.GetByLabel(AppStrings.NewPassword, new() { Exact = true }).FillEnsuringStable(secondPassword);
        await page.GetByLabel(AppStrings.ConfirmNewPassword, new() { Exact = true }).FillEnsuringStable(secondPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ChangePasswordButtonText }).ClickAsync();

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.PasswordChangedSuccessfullyMessage)).ToBeVisibleAsync();
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();

        await SignOut(page);

        // 3. The password she replaced is refused. Asserting this is what the success message above cannot do: it
        //    shows the moment the request returns, whichever of the two the account now holds.
        await TypeCredentials(page, email, firstPassword);
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.InvalidUserCredentials)).ToBeVisibleAsync();

        // 4. The new one works. The panel kept her address, so only the password is retyped.
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(secondPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
        await Expect(page).Not.ToHaveURLAsync(SignInPageUrl);
    }

    /// <summary>
    /// <list type="number">
    /// <item>A brand-new account: an e-mail address and nothing else.</item>
    /// <item>On Settings &gt; Account &gt; Phone she claims a phone number. That takes TWO proofs and this walks both:
    /// a code to the identifier she already holds (the elevated access prompt, mailed to her address) and then a code
    /// to the number she is claiming, texted to it. See <c>UserController.ChangeUserName</c> for why either one alone
    /// is worthless.</item>
    /// <item>The number is an identifier of the account now, so the Phone tab of the sign-in page gets her back in -
    /// with the code from the text message, which is a different token from the magic link the same request mails.</item>
    /// <item>On Settings &gt; Account &gt; Email she moves the account onto another address, the same two proofs over
    /// again. This time the elevated access code is read from the sms, which only works because the number she added
    /// above really did become a confirmed identifier (See <c>UserController.SendElevatedAccessToken</c>).</item>
    /// <item>The new address is the account's: the magic link mailed to it signs her in when it is opened.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task User_Should_MoveHerAccountOntoANewPhoneAndEmail_AndSignInThroughEachOfThem()
    {
        await SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken)).McpClient!;
        var email = NewTestEmail();
        var newEmail = NewTestEmail();
        var phoneNumber = NewTestPhoneNumber();

        RegisterForCleanup(() => DeleteAccounts([email, newEmail], [phoneNumber]));

        var page = await OpenApp(App.AdminPanel);
        await SignInNewUser(page, email, firstPassword, mcp);

        // ---- The phone number ----
        await page.GoToInApp(AccountSectionUrl);
        await page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Phone, Exact = true }).ClickAsync();
        await FillPhoneNumber(page.GetByPlaceholder(AppStrings.NewPhoneNumberPlaceholder), phoneNumber);

        var mailedBefore = await mcp.HangfireJobIds(email, cancellationToken);
        var textedBefore = await mcp.HangfireJobIds(phoneNumber, cancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();

        // The first proof: her e-mail is the only identifier she has, so that is where the elevated access code goes.
        await FillElevatedAccessIfPrompted(page, email, mailedBefore, mcp);

        // The second: a code texted to the number itself, typed into the tab's own box rather than "the first one on
        // the page" - the two factor section is collapsed on this page but rendered, and its box comes first.
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)).ToBeVisibleAsync();
        var changePhoneCode = await WaitForSixDigit(mcp, phoneNumber, textedBefore);
        await BitOtpInputUtils.FillOtpInputs(FormOf(page, AppStrings.PhoneTokenConfirmButtonText), changePhoneCode);

        // Changing an identifier rotates the security stamp, so every device is about to be signed out.
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();

        // ---- Signing in with the code the number receives ----
        await SignOut(page);
        await SignInWithPhoneOtp(page, mcp, phoneNumber);

        // ---- The new e-mail address ----
        await WaitForTheSignOutWarningToClear(page);
        await page.GoToInApp(AccountSectionUrl);
        await page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Email, Exact = true }).ClickAsync();
        await page.GetByPlaceholder(AppStrings.NewEmailPlaceholder).FillEnsuringStable(newEmail);

        textedBefore = await mcp.HangfireJobIds(phoneNumber, cancellationToken);
        var newAddressMailedBefore = await mcp.HangfireJobIds(newEmail, cancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();

        // The elevated access code now goes to both of her identifiers, and reading it off the sms is the assertion
        // that the number really is one of them - an unconfirmed number is skipped.
        await FillElevatedAccessIfPrompted(page, phoneNumber, textedBefore, mcp);

        // The second proof goes to the address being claimed, so that is the mailbox it is read from.
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SuccessfulSendChangeEmailTokenMessage)).ToBeVisibleAsync();
        var changeEmailCode = await WaitForSixDigit(mcp, newEmail, newAddressMailedBefore);
        await BitOtpInputUtils.FillOtpInputs(FormOf(page, AppStrings.EmailTokenConfirmButtonText), changeEmailCode);

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();

        // ---- Signing in by opening the magic link that address receives ----
        await SignOut(page);
        await SignInWithMagicLink(page, mcp, newEmail);
    }

    /// <summary>
    /// <list type="number">
    /// <item>A brand-new account turns two factor authentication on: the test reads the shared secret out of the QR's
    /// <c>otpauth://</c> link and, playing the authenticator app with <c>Otp.NET</c>, types the code that enrols it.</item>
    /// <item>Signing in now takes both steps - the mailed one-time-password only clears the first, and the panel asks
    /// for the authenticator's code.</item>
    /// <item>Turning it off is gated on elevated access, because it WEAKENS the account (See
    /// <c>UserController.TwoFactorAuth</c>) - but a two factor sign-in elevates the session from its first moment, so
    /// no code is asked for and none is sent. Enabling is the asymmetric one: it needs no elevation, since a valid
    /// authenticator code is the stronger proof already.</item>
    /// <item>With it off, the one-time-password alone signs her in again - reaching the home page at all is what says
    /// so, since with the second factor on it never got her past the panel.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task User_Should_Enable2fa_SignInWithIt_AndDisableItAgain()
    {
        await SkipWithoutGlobalAdminCredentials();

        var cancellationToken = TestContext.CancellationToken;
        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken)).McpClient!;
        var email = NewTestEmail();

        RegisterForCleanup(() => DeleteAccounts([email], []));

        var page = await OpenApp(App.AdminPanel);
        await SignInNewUser(page, email, firstPassword, mcp);

        // 1. Enrol the authenticator. The QR image links to an otpauth:// uri whose "secret" is the raw shared key -
        //    the same one an authenticator app imports.
        await page.GoToInApp($"{PageUrls.Settings}/{PageUrls.SettingsSections.Tfa}");

        var qrLink = page.Locator("a[href^='otpauth']").First;
        await qrLink.WaitForAsync();
        var authenticatorUri = await qrLink.GetAttributeAsync("href") ?? "";
        var sharedKey = Regex.Match(authenticatorUri, "secret=([^&]+)").Groups[1].Value;
        Assert.IsFalse(string.IsNullOrWhiteSpace(sharedKey), $"No shared key in the QR link '{authenticatorUri}'.");

        // Filling the last digit raises the input's OnFill, which is wired to the same handler as the Verify button -
        // clicking that as well races a handler already in flight (See TwoFactorSection.razor).
        await BitOtpInputUtils.FillOtpInputs(page, TwoFactorCode(sharedKey));

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.TwoFactorAuthenticationEnabled)).ToBeVisibleAsync();
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();

        // 2. The mailed one-time-password is only the first step now.
        await SignOut(page);
        await SignInWithEmailOtp(page, mcp, email, sharedKey);

        // 3. Turn it off. With 2fa on the section renders a pivot instead of the enrolment steps, and the button lives
        //    on its "Disable" tab.
        var mailedBefore = await mcp.HangfireJobIds(email, cancellationToken);

        await WaitForTheSignOutWarningToClear(page);
        await page.GoToInApp($"{PageUrls.Settings}/{PageUrls.SettingsSections.Tfa}");
        await page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.TfaDisable2faHeader, Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaDisable2faButtonText }).ClickAsync();

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.TwoFactorAuthenticationDisabled)).ToBeVisibleAsync();
        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();

        // No prompt was answered above, and nothing was sent to answer it with: the two factor sign-in had already
        // elevated the session.
        var mailedSince = await mcp.HangfireJobIds(email, cancellationToken);
        Assert.AreSequenceEqual(mailedBefore, mailedSince,
            "Disabling two factor authentication sent a new message, so the session was not already elevated by the two factor sign-in.");

        // 4. The one-time-password alone is enough again.
        await SignOut(page);
        await SignInWithEmailOtp(page, mcp, email);
    }

    /// <summary>
    /// <list type="number">
    /// <item>A CDP virtual <b>platform</b> authenticator is attached to the page (auto-approving user presence and user
    /// verification), so <c>navigator.credentials.create()</c> / <c>get()</c> complete headlessly - Playwright cannot
    /// touch a real fingerprint sensor. Chromium only, so any other browser skips.</item>
    /// <item>On Settings &gt; Account &gt; Passwordless she enrols a passkey. Enrolment adds a NEW way in, so it is
    /// gated on elevated access (See <c>UserController.CreateWebAuthnCredential</c>) and the mailed code is answered
    /// first; the ceremony then registers the credential and the button flips to its "disable" state.</item>
    /// <item>She signs out - which clears the tokens but not the <c>bit-webauthn</c> marker - and the fingerprint
    /// button on the sign-in page signs her straight back in.</item>
    /// <item>She turns it off again. That ceremony is a <c>credentials.get()</c>, so it needs no elevated access: she
    /// has already proved she holds the passkey being removed.</item>
    /// <item>Both halves of the enrolment are really gone: the credential row on the server, and the marker in this
    /// browser - so the sign-in page stops offering the passkey at all.</item>
    /// </list>
    /// <para>
    /// The relying party id is the deployment's own domain, derived per request from the origin the app declares (See
    /// <c>HttpRequestExtensions.GetWebAppUrl</c>) - which is the point of running this here rather than against a
    /// loopback server, where it has to be talked into being "localhost".
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task User_Should_EnablePasswordless_SignInWithThePasskey_AndDisableItAgain()
    {
        await SkipWithoutGlobalAdminCredentials();

        if (Browser.BrowserType.Name is not "chromium")
        {
            Assert.Inconclusive("The WebAuthn virtual authenticator is only available through a Chromium CDP session.");
            return;
        }

        var cancellationToken = TestContext.CancellationToken;
        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken)).McpClient!;
        var email = NewTestEmail();

        RegisterForCleanup(() => DeleteAccounts([email], []));

        var page = await OpenApp(App.AdminPanel);

        // The authenticator lives on the page's target, so it survives every navigation below.
        await AddVirtualAuthenticator(page);

        await SignInNewUser(page, email, firstPassword, mcp);

        // 1. Enrol the passkey. Passwordless is the account section's first (default) tab, so its button is already up.
        await page.GoToInApp(AccountSectionUrl);

        var mailedBefore = await mcp.HangfireJobIds(email, cancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EnablePasswordless }).ClickAsync();
        await FillElevatedAccessIfPrompted(page, email, mailedBefore, mcp);

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.EnablePasswordlessSucsessMessage)).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.DisablePasswordless })).ToBeVisibleAsync();

        var userId = await GetUserId(page);

        // 2. Sign out and back in with the passkey. The button is icon-only (a Fingerprint), and it shows up once the
        //    panel's first render has found a configured credential (See SignInPanel.OnAfterFirstRenderAsync).
        await SignOut(page);
        await page.GoToInApp(PageUrls.SignIn);
        await page.Locator("button:has(.bit-icon--Fingerprint)").ClickAsync();
        await Expect(page).Not.ToHaveURLAsync(SignInPageUrl);

        // 3. Turn it off.
        await page.GoToInApp(AccountSectionUrl);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.DisablePasswordless }).ClickAsync();

        await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.DisablePasswordlessSucsessMessage)).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EnablePasswordless })).ToBeVisibleAsync();

        // 4. The credential is gone from the server, not merely forgotten by this browser - the message above shows
        //    either way, since PasswordlessTab clears its local marker in a finally block.
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(cancellationToken);
        await using (var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(cancellationToken))
        {
            var remainingCredentials = await dbContext.WebAuthnCredential.IgnoreQueryFilters()
                .CountAsync(credential => credential.UserId == userId, cancellationToken);

            Assert.AreEqual(0, remainingCredentials,
                "Disabling passwordless sign-in must delete the credential row, otherwise the account keeps a way in that no " +
                "page of the app lists and that neither a password change nor revoking every session touches.");
        }

        // 5. And this browser no longer remembers one, so the sign-in page stops offering it. Read out of local
        //    storage (the ids WebAuthnServiceBase keeps under "bit-webauthn"), because the button's mere absence would
        //    also "hold" on a page that simply never got as far as rendering it.
        await SignOut(page);
        await page.GoToInApp(PageUrls.SignIn);

        var configuredUserIds = await page.EvaluateAsync<string?>("() => localStorage.getItem('bit-webauthn')");
        Assert.DoesNotContain(userId.ToString(), configuredUserIds ?? "",
            "The account is still marked as passkey-configured in this browser, so the sign-in page would keep offering a " +
            "credential the server has already deleted.");

        await Expect(page.Locator("button:has(.bit-icon--Fingerprint)")).ToBeHiddenAsync();
    }

    /// <summary>
    /// Enables the CDP WebAuthn domain and attaches a virtual <b>platform</b> ("internal") authenticator that
    /// auto-confirms user presence and user verification, so both ceremonies complete without a device or a gesture.
    /// The transport matches the server's <c>AuthenticatorAttachment.Platform</c>, and <c>isUserVerified</c> satisfies
    /// its <c>UserVerification.Required</c>.
    /// </summary>
    private static async Task AddVirtualAuthenticator(IPage page)
    {
        var cdp = await page.Context.NewCDPSessionAsync(page);

        await cdp.SendAsync("WebAuthn.enable");

        await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new Dictionary<string, object>
            {
                ["protocol"] = "ctap2",
                ["transport"] = "internal",             // platform authenticator - matches AuthenticatorAttachment.Platform
                ["hasResidentKey"] = true,
                ["hasUserVerification"] = true,
                ["isUserVerified"] = true,              // user verification always satisfied (the options require it)
                ["automaticPresenceSimulation"] = true, // user presence auto-confirmed - no touch needed
            }
        });
    }

    /// <summary>
    /// Sets a new password through the pages a user with no way in would use: the sign-in page's "Forgot password?"
    /// link, which asks for the address, and the reset password page it lands on, which takes the code and the new
    /// password. The code is read from the mail that request sent.
    /// </summary>
    private async Task ResetPassword(IPage page, McpClient mcp, string email, string password)
    {
        await page.GoToInApp(PageUrls.SignIn);

        // The link sits in the password field's own label (See SignInPanel.razor), and renders as an <a>.
        await page.GetByRole(AriaRole.Link, new() { Name = AppStrings.ForgotPasswordLink }).ClickAsync();
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Submit }).ClickAsync();

        // Sending it navigates to the reset password page with the address in the query - client side, so the url is
        // what says the page has arrived.
        await Expect(page).ToHaveURLAsync(new Regex(Regex.Escape(PageUrls.ResetPassword), RegexOptions.IgnoreCase));

        var code = await WaitForSixDigit(mcp, email, mailedBefore);

        // The page renders a single OTP input, and filling its last digit swaps the token step for the password one.
        await BitOtpInputUtils.FillOtpInputs(page, code);

        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);
        await page.GetByPlaceholder(AppStrings.ConfirmPassword).FillEnsuringStable(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.ResetPasswordButtonText }).ClickAsync();

        await Expect(page.GetByText(AppStrings.ResetPasswordSuccessTitle)).ToBeVisibleAsync();
    }

    /// <summary>Signs in with an address and a password, and waits until the app has let her off the sign-in page.</summary>
    private async Task SignInWithPassword(IPage page, string email, string password)
    {
        await TypeCredentials(page, email, password);

        await Expect(page).Not.ToHaveURLAsync(SignInPageUrl);
    }

    /// <summary>
    /// Asks the sign-in page for a code and types the one that was mailed, then the authenticator's if the account has
    /// a second factor - which is what <paramref name="twoFactorSharedKey"/> being given means. Left null, reaching the
    /// app at all is the assertion that the one-time-password was enough on its own.
    /// </summary>
    private async Task SignInWithEmailOtp(IPage page, McpClient mcp, string email, string? twoFactorSharedKey = null)
    {
        await page.GoToInApp(PageUrls.SignIn);
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendMagicLinkButtonText }).ClickAsync();
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var code = await WaitForSixDigit(mcp, email, mailedBefore);
        await BitOtpInputUtils.FillOtpInputs(page, code);

        if (twoFactorSharedKey is not null)
        {
            await Expect(page.GetByText(AppStrings.TfaPanelTitle)).ToBeVisibleAsync();
            await BitOtpInputUtils.FillOtpInputs(page, TwoFactorCode(twoFactorSharedKey));
        }

        await Expect(page).Not.ToHaveURLAsync(SignInPageUrl);
    }

    /// <summary>
    /// Asks the sign-in page's Phone tab for a code and types the one that was texted. The panel showing its boxes is
    /// what says <c>SendOtp</c> has returned, so the newest message to this number is this sign-in's code.
    /// </summary>
    private async Task SignInWithPhoneOtp(IPage page, McpClient mcp, string phoneNumber)
    {
        await page.GoToInApp(PageUrls.SignIn);

        await page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Phone, Exact = true }).ClickAsync();
        await FillPhoneNumber(page.GetByPlaceholder(AppStrings.PhoneNumberPlaceholder), phoneNumber);

        var textedBefore = await mcp.HangfireJobIds(phoneNumber, TestContext.CancellationToken);

        // The same button reads "Send OTP" on this tab: there is no magic link to text.
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendOtpButtonText }).ClickAsync();
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var code = await WaitForSixDigit(mcp, phoneNumber, textedBefore);
        await BitOtpInputUtils.FillOtpInputs(page, code);

        await Expect(page).Not.ToHaveURLAsync(SignInPageUrl);
    }

    /// <summary>
    /// Asks for the magic link and then OPENS it, rather than typing the code beside it in the same mail: the link is
    /// a real navigation into <c>/sign-in?otp=...</c>, which the panel spends on arrival (See SignInPanel.OnInitAsync)
    /// and then leaves for the return url.
    /// </summary>
    private async Task SignInWithMagicLink(IPage page, McpClient mcp, string email)
    {
        await page.GoToInApp(PageUrls.SignIn);
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(email);

        var mailedBefore = await mcp.HangfireJobIds(email, TestContext.CancellationToken);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendMagicLinkButtonText }).ClickAsync();
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var mail = await mcp.WaitForHangfireJob(email, mailedBefore, TestContext.CancellationToken);
        var magicLink = mail.HttpLinksInArguments().FirstOrDefault(link => link.Contains("otp=", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(magicLink, $"The mail carried no sign-in link. Arguments: '{mail.DecodedArguments()}'.");

        await page.GotoAsync(magicLink, new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Expect(page).Not.ToHaveURLAsync(SignInPageUrl);
    }

    /// <summary>Fills the sign-in page's address and password and submits them, whatever the app answers.</summary>
    private async Task TypeCredentials(IPage page, string email, string password)
    {
        await page.GoToInApp(PageUrls.SignIn);

        var emailBox = page.GetByPlaceholder(AppStrings.EmailPlaceholder);

        await emailBox.FillEnsuringStable(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillEnsuringStable(password);

        // FillEnsuringStable watches one field: a hydration reset landing while the password is filled leaves the
        // address empty instead (See AppTestBase.SignIn).
        if (await emailBox.InputValueAsync() != email)
            await emailBox.FillEnsuringStable(email);

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();
    }

    /// <summary>
    /// Waits out an all-devices warning still on screen before an action that raises another one. It dismisses itself
    /// after ten seconds (See AppSnackBar) and nothing in these journeys reloads the page, so the previous one often
    /// outlives the step that raised it - and two of the same message is a strict mode violation rather than a pass.
    /// Waiting is also what makes the assertion afterwards about the step that follows rather than the one before.
    /// </summary>
    private async Task WaitForTheSignOutWarningToClear(IPage page)
        => await Expect(BitSnackBarUtils.GetSnackBar(page, AppStrings.SignOutOfAllDevicesWarningMessage)).ToHaveCountAsync(0);

    /// <summary>Signs out through the header's app menu and the dialog that confirms it.</summary>
    private async Task SignOut(IPage page)
    {
        await ClickAppMenuItem(page, AppStrings.SignOut);

        // SignOutConfirmDialog's OK: the menu's own "Sign out" closed with the menu, and role lookups skip hidden elements.
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut, Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Link, new() { Name = AppStrings.SignIn }).First).ToBeVisibleAsync();
    }

    /// <summary>
    /// Types an E.164 number into a <c>BitPhoneInput</c> and waits until the component has taken it: it parses what
    /// was typed on its change round-trip - which the blur raises - keeping only the local digits in the box and
    /// moving the dialing code into the country button. Waiting for that is what stops the submit which follows from
    /// racing the round-trip and posting an empty number.
    /// </summary>
    private async Task FillPhoneNumber(ILocator phoneInput, string phoneNumber)
    {
        await phoneInput.FillAsync(phoneNumber);
        await phoneInput.BlurAsync();

        await Expect(phoneInput).ToHaveValueAsync(phoneNumber["+1".Length..]);
    }

    /// <summary>
    /// The form (an <c>EditForm</c>) whose submit button reads <paramref name="submitButtonText"/> - the scope an OTP
    /// is typed into, since every box on the page looks alike.
    /// </summary>
    private static ILocator FormOf(IPage page, string submitButtonText)
        => page.Locator("form", new() { Has = page.GetByRole(AriaRole.Button, new() { Name = submitButtonText }) });

    /// <summary>
    /// The authenticator app's current code for a Base32 shared key. Identity's authenticator is standard TOTP
    /// (HMAC-SHA1, 6 digits, 30s step), which is <c>Otp.NET</c>'s default.
    /// </summary>
    private static string TwoFactorCode(string base32SharedKey) => new Totp(Base32Encoding.ToBytes(base32SharedKey)).ComputeTotp();

    private static string NewTestEmail() => $"e2e-{Guid.NewGuid():N}@bitplatform.dev";

    /// <summary>
    /// A number in the +1 555-0100..555-0199 range North America reserves for fiction, so the deployment's live Twilio
    /// account can never text a real person. Random within it, and unique per run, because these accounts are found by
    /// their identifiers.
    /// </summary>
    private static string NewTestPhoneNumber()
        => FormattableString.Invariant($"+1{Random.Shared.Next(200, 1000)}555{Random.Shared.Next(100, 200):D4}");

    private static string AccountSectionUrl => $"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}";

    private static Regex SignInPageUrl => new(Regex.Escape(PageUrls.SignIn), RegexOptions.IgnoreCase);

    /// <summary>
    /// Deletes whichever account ended up holding any of these identifiers, with everything hanging off it. Not by the
    /// address it started with, because a journey that moves the account onto another one leaves it findable only by
    /// the new one - and not on the test's own token, because a canceled or timed out test still owes the deployment
    /// its cleanup.
    /// </summary>
    private static async Task DeleteAccounts(string[] emails, string[] phoneNumbers)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var normalizedEmails = emails.Select(email => email.ToUpperInvariant()).ToArray();

        var userIds = await dbContext.Users.IgnoreQueryFilters()
            .Where(user => (user.NormalizedEmail != null && normalizedEmails.Contains(user.NormalizedEmail))
                           || (user.PhoneNumber != null && phoneNumbers.Contains(user.PhoneNumber)))
            .Select(user => user.Id)
            .ToListAsync(CancellationToken.None);

        if (userIds.Count is 0)
            return; // A journey that failed before its account existed has nothing to undo.

        var sessionIds = await dbContext.UserSessions.IgnoreQueryFilters()
            .Where(session => userIds.Contains(session.UserId))
            .Select(session => session.Id)
            .ToListAsync(CancellationToken.None);

        if (sessionIds.Count > 0)
        {
            await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters()
                .Where(subscription => subscription.UserSessionId != null && sessionIds.Contains(subscription.UserSessionId.Value))
                .ExecuteDeleteAsync(CancellationToken.None);
        }

        await dbContext.UserSessions.IgnoreQueryFilters().Where(session => userIds.Contains(session.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.TenantUsers.IgnoreQueryFilters().Where(membership => userIds.Contains(membership.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.UserRoles.IgnoreQueryFilters().Where(role => userIds.Contains(role.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.UserTokens.IgnoreQueryFilters().Where(token => userIds.Contains(token.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.WebAuthnCredential.IgnoreQueryFilters().Where(credential => userIds.Contains(credential.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await dbContext.Users.IgnoreQueryFilters().Where(user => userIds.Contains(user.Id)).ExecuteDeleteAsync(CancellationToken.None);
    }
}
