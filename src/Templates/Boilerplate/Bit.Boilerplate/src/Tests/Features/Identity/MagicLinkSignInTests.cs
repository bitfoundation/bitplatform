using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components.Pages.Identity;
using Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The two ways a magic link e-mail signs a brand-new account in - typing the code, or opening the link - rendered with
/// bUnit rather than a browser. Both still run against a real <see cref="AppTestServer"/>: the account is provisioned,
/// the e-mail is captured, and the code / link are the ones the server actually issued. What is skipped is only the
/// browser around the panel, which neither of these behaviors depends on. <see cref="UITests"/> keeps the equivalent
/// full end-to-end journey, and <see cref="BunitUITests"/> explains the trade-off between the two flavours.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class MagicLinkSignInTests
{
    /// <summary>
    /// A brand-new user signs in with her e-mail using the one-time-password: she asks for the code on the sign-in panel,
    /// the test reads it from the confirmation e-mail the server captured, and typing the 6 digits signs her in.
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingEmailedOtpCode()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var email = MagicLinkSignInUtils.NewTestEmail();

        // The panel is rendered with an explicit ReturnUrl on purpose: bUnit's NavigationManager starts at
        // "http://localhost/", which is byte-identical to ToAbsoluteUri(PageUrls.Home), so asserting a redirect to the
        // home page would hold before any navigation happened and could never catch a missing one.
        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<SignInPanel>(panel => panel
                .Add(p => p.SignInPanelType, SignInPanelType.Full)
                .Add(p => p.ReturnUrl, PageUrls.Settings)));

        // .Change() drives the field's (non-debounced) onchange path, so the two-way bound model is updated
        // synchronously in C# - no browser and no waiting out the 500ms debounce.
        cut.Find($"input[placeholder='{AppStrings.EmailPlaceholder}']").Change(email);

        // The magic link button stays disabled until the model carries an e-mail (See SignInPanel.razor), which the
        // change above has just done. Its label is the Email tab's, since Email is the panel's default tab.
        cut.FindAll("button")
           .Single(button => button.TextContent.Contains(AppStrings.SendMagicLinkButtonText, StringComparison.Ordinal))
           .Click();

        // A brand-new address makes the server register the (still unconfirmed) account, e-mail the confirmation link +
        // OTP and answer "not confirmed" - which is what swaps the credentials form for the OTP panel.
        cut.WaitForAssertion(() => cut.Find(".bit-otp-inp"), timeout: TimeSpan.FromSeconds(30));

        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        Assert.MatchesRegex(new Regex(@"^\d{6}$"), captured.Token!,
            "The one-time-password read from the confirmation e-mail should be a 6 digit code.");

        BitOtpInputUtils.FillOtpInputs(cut.Find(".bit-otp-inp"), captured.Token!);

        await AssertSignedInAndReturnedTo(ctx, cut, email, PageUrls.Settings);
    }

    /// <summary>
    /// A brand-new user signs in by opening the magic link from her e-mail instead of typing the OTP: asking for it
    /// provisions the account and e-mails the link, and opening that very link - path and query exactly as the server
    /// wrote them - confirms her e-mail, signs her in and brings her to the return-url the request carried.
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingMagicLink()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var email = MagicLinkSignInUtils.NewTestEmail();

        // Requesting the link is the arrange step here (the panel doing it is what the OTP test above covers), so it goes
        // straight through the controller. A brand-new address auto-provisions the unconfirmed account, e-mails the magic
        // link carrying this return-url, and reports the account as unconfirmed.
        var identityController = ctx.Services.GetRequiredService<IIdentityController>();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, PageUrls.Settings, TestContext.CancellationToken));

        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        // Opening the captured link's own path AND query is what makes this a test of the link the server built rather
        // than of a url the test assembled - and [SupplyParameterFromQuery] can only read a real query string anyway.
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(captured.Link!.PathAndQuery);

        // ConfirmPage.OnInitAsync auto-submits the confirmation for the e-mail + token it finds in that query string.
        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters.AddChildContent<ConfirmPage>());

        await AssertSignedInAndReturnedTo(ctx, cut, email, PageUrls.Settings);
    }

    /// <summary>
    /// Asserts the outcome both flows share: the tokens the server issued were stored (so the cascaded authentication
    /// state now reports this very account), and the user was taken to <paramref name="returnUrl"/>.
    /// </summary>
    private static async Task AssertSignedInAndReturnedTo(BunitContext ctx, IRenderedComponent<CascadingAuthenticationState> cut, string email, string returnUrl)
    {
        var authenticationStateProvider = ctx.Services.GetRequiredService<AuthenticationStateProvider>();

        await cut.WaitForAssertionAsync(async () =>
        {
            var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
            Assert.IsTrue(user.IsAuthenticated(), "Storing the tokens should have raised AuthenticationStateChanged.");
            Assert.AreEqual(email, user.GetEmail(), "The signed-in account should be the one the e-mail was sent to.");
        }, timeout: TimeSpan.FromSeconds(30));

        // Both flows navigate only after StoreTokens has completed, so this cannot be asserted before the wait above -
        // and it needs a wait of its own because that wait returns on the token store, not on the navigation.
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();

        await cut.WaitForAssertionAsync(() =>
        {
            Assert.AreEqual(navigationManager.ToAbsoluteUri(returnUrl).ToString(), navigationManager.Uri,
                "A successful sign-in should have navigated to the return-url the request carried.");
        }, timeout: TimeSpan.FromSeconds(30));
    }

    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;
}
