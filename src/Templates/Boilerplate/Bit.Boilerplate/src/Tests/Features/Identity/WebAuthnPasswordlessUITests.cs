namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class WebAuthnPasswordlessUITests : AppPageTest
{
    /// <summary>
    /// End-to-end WebAuthn / passwordless (passkey) journey for a brand-new user, driven through a Chrome DevTools
    /// virtual authenticator (Playwright can't touch a real fingerprint sensor):
    /// <list type="number">
    /// <item>A CDP virtual <b>platform</b> authenticator is attached to the page (auto-approving user presence and user
    /// verification), so <c>navigator.credentials.create()</c> / <c>get()</c> succeed headlessly - no device, no gesture.</item>
    /// <item>She signs in for the first time with a magic link OTP (the same flow as <see cref="MagicLinkSignInTests"/>).</item>
    /// <item>On Settings &gt; Account &gt; Passwordless she clicks "Enable passwordless sign-in". Enrolment is a
    /// privileged operation, so she first answers the elevated access OTP prompt; the ceremony then registers a
    /// credential (<c>credentials.create()</c>) against the virtual authenticator and stores it on the server and in local
    /// storage; the success snackbar shows and the button flips to its "disable" state.</item>
    /// <item>She signs out - which clears only the auth tokens; the <c>bit-webauthn</c> local-storage marker survives, so
    /// the sign-in page will still offer the passkey button.</item>
    /// <item>On the sign-in page she clicks the fingerprint (passkey) button, which runs <c>credentials.get()</c> against
    /// the virtual authenticator and signs her straight back in - the home page shows her persona.</item>
    /// </list>
    /// The web app runs under <c>http://localhost:&lt;port&gt;</c> (See <see cref="AppTestServer.WebAppServerAddress"/>):
    /// Chrome refuses an IP literal as a WebAuthn RP ID, and the server derives its RP ID and allowed origin per request
    /// from the caller's origin (See <c>HttpRequestExtensions.GetWebAppUrl</c>), so the RP ID is "localhost" - matching
    /// the browser's origin.
    /// </summary>
    [TestMethod]
    public async Task User_Should_EnablePasswordless_AndSignInWithPasskey()
    {
        // The CDP virtual authenticator (WebAuthn.addVirtualAuthenticator) is a Chromium-only capability.
        if (Browser.BrowserType.Name is not "chromium")
        {
            Assert.Inconclusive("The WebAuthn virtual authenticator is only available through a Chromium CDP session.");
            return;
        }

        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        // Attach the virtual authenticator before any credential ceremony runs.
        await AddVirtualAuthenticator(Page);

        var email = MagicLinkSignInUtils.NewTestEmail();

        // 1. First sign-in with the magic link OTP registers and signs in the brand-new account (no passkey yet).
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);

        // 2. Enable passwordless sign-in on the account settings page. Navigating to /settings/account expands the
        //    account accordion, whose first (default) pivot tab is Passwordless, so the "Enable" button is already shown.
        await Page.GotoAsync(new Uri(server.WebAppServerAddress, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The page is on screen before the app is listening to it, so a click landing in that window is simply lost.
        await Page.WaitForBlazorInteractive();
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EnablePasswordless }).ClickAsync();

        // Enrolling a passkey is a privileged operation: PasswordlessTab.EnablePasswordless calls
        // AuthManager.TryEnterElevatedAccessMode BEFORE the ceremony, and a session that signed in through a magic link
        // OTP is not elevated, so the ElevatedAccessModal opens and e-mails a code by itself (the account has no
        // authenticator app). Nothing else happens until it is answered - credentials.create() is never reached and no
        // snackbar ever shows. (See WebAuthnEnrolmentElevationTests for the server side half of the same rule.)
        // The code goes into the modal's own BitOtpInput, addressed through the modal (".bit-mdl"): this page already
        // renders another one in its two-factor section, and that one comes first in the DOM, so filling "the page's
        // OTP input" submits the code as a 2fa enable attempt and leaves the modal unanswered.
        var elevatedAccessPrompt = Page.Locator(".bit-mdl", new() { Has = Page.Locator(".elevated-access") });
        await Expect(elevatedAccessPrompt).ToBeVisibleAsync();

        var elevatedAccessEmail = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.ElevatedAccess, TestContext.CancellationToken);

        await BitOtpInputUtils.FillOtpInputs(elevatedAccessPrompt, elevatedAccessEmail.Token!);

        // credentials.create() against the virtual authenticator succeeds: the success snackbar shows and the button
        // flips to its "disable" state (isConfigured == true).
        await Expect(BitSnackBarUtils.GetSnackBar(Page, AppStrings.EnablePasswordlessSucsessMessage)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.DisablePasswordless })).ToBeVisibleAsync();

        // 3. She signs out. Sign-out clears only the auth tokens; the bit-webauthn marker survives, so the passkey option
        //    will still be offered at the next sign-in.
        await SignOut(Page);

        // 4. Sign back in with the passkey.
        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The passwordless button is icon-only (BitIconName.Fingerprint); Bit renders the icon as
        // <i class="bit-icon bit-icon--Fingerprint">. It appears once SignInPanel's first render has confirmed a
        // configured credential exists (See SignInPanel.OnAfterFirstRenderAsync -> showWebAuthn). It is the only
        // Fingerprint icon on the sign-in page, so this selector is unambiguous.
        // The page is on screen before the app is listening to it, so a click landing in that window is simply lost.
        await Page.WaitForBlazorInteractive();
        await Page.Locator("button:has(.bit-icon--Fingerprint)").ClickAsync();

        // credentials.get() against the virtual authenticator completes the sign-in and redirects home as her.
        // The account has no 2FA, so no two-factor panel appears.
        await Page.WaitForURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
    }

    /// <summary>
    /// Enables the CDP WebAuthn domain on the page's session and attaches a virtual <b>platform</b> ("internal")
    /// authenticator that auto-confirms user presence and user verification, so both the registration
    /// (<c>navigator.credentials.create()</c>) and the assertion (<c>navigator.credentials.get()</c>) ceremonies
    /// complete without a real authenticator or any user gesture. The "internal" transport matches the server's
    /// <c>AuthenticatorAttachment.Platform</c> requirement, and <c>isUserVerified</c> satisfies its
    /// <c>UserVerification.Required</c>. The authenticator (and the credential registered into it) lives on the page's
    /// target for the rest of the test, surviving the sign-out navigation.
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
                ["isUserVerified"] = true,              // user verification always satisfied (options require it)
                ["automaticPresenceSimulation"] = true, // user presence auto-confirmed - no touch needed
            }
        });
    }

    /// <summary>Signs the current user out through the header persona menu and its confirmation dialog.</summary>
    private async Task SignOut(IPage page)
    {
        // Open the user menu in the header (clicking its persona) then click its "Sign out" action.
        await page.Locator(".bit-prs.persona").First.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();

        // Confirm in the dialog (its OK button is also labelled "Sign out"; the menu one is gone once the dialog is up).
        await Expect(page.GetByText(AppStrings.SignOutPrompt)).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).Last.ClickAsync();

        // Signing out clears the tokens and closes the dialog; wait until the confirmation prompt is gone.
        await Expect(page.GetByText(AppStrings.SignOutPrompt)).ToBeHiddenAsync();
    }
}
