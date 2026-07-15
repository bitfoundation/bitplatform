using Boilerplate.Tests.Infrastructure.Components;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class WebAuthnPasswordlessUITests : AppPageTest
{
    /// <summary>
    /// End-to-end WebAuthn / passwordless (passkey) journey for a brand-new user, driven through a Chrome DevTools
    /// virtual authenticator (Playwright can't touch a real fingerprint sensor):
    /// <list type="number">
    /// <item>A CDP virtual <b>platform</b> authenticator is attached to the page (auto-approving user presence and user
    /// verification), so <c>navigator.credentials.create()</c> / <c>get()</c> succeed headlessly - no device, no gesture.</item>
    /// <item>She signs in for the first time with a magic link OTP (the same flow as <see cref="MagicLinkSignInTests"/>).</item>
    /// <item>On Settings &gt; Account &gt; Passwordless she clicks "Enable passwordless sign-in", which registers a
    /// credential (<c>credentials.create()</c>) against the virtual authenticator and stores it on the server and in local
    /// storage; the success snackbar shows and the button flips to its "disable" state.</item>
    /// <item>She signs out - which clears only the auth tokens; the <c>bit-webauthn</c> local-storage marker survives, so
    /// the sign-in page will still offer the passkey button.</item>
    /// <item>On the sign-in page she clicks the fingerprint (passkey) button, which runs <c>credentials.get()</c> against
    /// the virtual authenticator and signs her straight back in - the home page shows her persona.</item>
    /// </list>
    /// The whole test runs on the <c>http://localhost:&lt;port&gt;</c> alias of the loopback test server (not its
    /// <c>127.0.0.1</c> address): Chrome refuses an IP literal as a WebAuthn RP ID, and the server derives its RP ID and
    /// allowed origin per-request from the Host header (See <c>HttpRequestExtensions.GetWebAppUrl</c>), so a "localhost"
    /// Host makes the RP ID "localhost" - matching the browser's origin - with no server-side configuration override.
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

        // A bare server (no ClientBrowserContext) so that we - not AppTestServer.Start - own the WebAssembly
        // ServerAddress init script and can point it at the localhost alias below (a single, consistent origin).
        await using var server = new AppTestServer();

        // Same loopback server, reached through its "localhost" host so the WebAuthn RP ID resolves to "localhost"
        // (Chrome rejects the raw 127.0.0.1 IP literal as an RP ID). Computed before Build so the ServerAddress override
        // below can point every internal API call at the same localhost origin.
        var appBaseUrl = new UriBuilder(server.WebAppServerAddress) { Host = "localhost" }.Uri;

        // The test host runs Blazor Server, so the WebAuthn options (and their RP ID) are produced by a SERVER-SIDE call
        // to the identity API through the app's internal HttpClient, whose base address is the ServerAddress config
        // (127.0.0.1 by default). Point that at the localhost origin so the RP ID the server derives from the request
        // Host (See HttpRequestExtensions.GetWebAppUrl -> Fido2Configuration.ServerDomain) matches the browser's
        // localhost origin - otherwise credentials.create() fails with "relying party ID is not ... the current domain".
        await server.Build(configureTestConfigurations: configuration => configuration["ServerAddress"] = appBaseUrl.ToString())
            .Start(TestContext.CancellationToken);

        // Also feed the localhost origin to the Blazor WebAssembly startup params, so the same holds if the host ever
        // runs the app in WebAssembly mode (where the API call - and thus the RP ID - comes from the browser instead).
        await SetBlazorWebAssemblyServerAddress(appBaseUrl, Context);

        // Attach the virtual authenticator before any credential ceremony runs.
        await AddVirtualAuthenticator(Page);

        var email = MagicLinkSignInUtils.NewTestEmail();

        // 1. First sign-in with the magic link OTP registers and signs in the brand-new account (no passkey yet).
        await SignInWithMagicLinkOtp(Page, server, appBaseUrl, email);

        // 2. Enable passwordless sign-in on the account settings page. Navigating to /settings/account expands the
        //    account accordion, whose first (default) pivot tab is Passwordless, so the "Enable" button is already shown.
        await Page.GotoAsync(new Uri(appBaseUrl, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Account}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.EnablePasswordless }).ClickAsync();

        // credentials.create() against the virtual authenticator succeeds: the success snackbar shows and the button
        // flips to its "disable" state (isConfigured == true).
        await Expect(Page.GetByText(AppStrings.EnablePasswordlessSucsessMessage)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.DisablePasswordless })).ToBeVisibleAsync();

        // 3. She signs out. Sign-out clears only the auth tokens; the bit-webauthn marker survives, so the passkey option
        //    will still be offered at the next sign-in.
        await SignOut(Page);

        // 4. Sign back in with the passkey.
        await Page.GotoAsync(new Uri(appBaseUrl, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The passwordless button is icon-only (BitIconName.Fingerprint); Bit renders the icon as
        // <i class="bit-icon bit-icon--Fingerprint">. It appears once SignInPanel's first render has confirmed a
        // configured credential exists (See SignInPanel.OnAfterFirstRenderAsync -> showWebAuthn). It is the only
        // Fingerprint icon on the sign-in page, so this selector is unambiguous.
        await Page.Locator("button:has(.bit-icon--Fingerprint)").ClickAsync();

        // credentials.get() against the virtual authenticator completes the sign-in and redirects home as her.
        // The account has no 2FA, so no two-factor panel appears.
        await Page.WaitForURLAsync(appBaseUrl.ToString());
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

    /// <summary>
    /// Signs a brand-new account in through the magic link OTP flow against the given (localhost) origin - the same
    /// steps as <see cref="MagicLinkSignInUtils.SignInViaMagicLinkOtp"/>, but navigating the localhost alias so the
    /// whole test stays on one origin (and one local-storage partition).
    /// </summary>
    private async Task SignInWithMagicLinkOtp(IPage page, AppTestServer server, Uri appBaseUrl, string email)
    {
        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(page, appBaseUrl, email);

        // A brand-new account's confirmation e-mail carries the OTP; we only need the code, not the (127.0.0.1-based) link.
        var (_, otpCode) = await MagicLinkSignInUtils.ReadConfirmationEmail(server, email, TestContext.CancellationToken);
        await BitOtpInputUtils.FillOtpInputs(page, otpCode);

        // Filling the last digit confirms the e-mail, signs her in and redirects to the home page.
        await page.WaitForURLAsync(appBaseUrl.ToString());
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
