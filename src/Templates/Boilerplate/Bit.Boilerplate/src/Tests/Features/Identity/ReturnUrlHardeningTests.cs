using System.Web;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Tests.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Guards the <c>return-url</c> trust boundary. The value travels a long way from an anonymous request body, through
/// a link the app itself e-mails, into <c>NavigationManager.NavigateTo</c> on the client - so anything that can leave
/// this origin has to be rejected somewhere, and <see cref="Uri.IsAppRelativeUrl"/> is that somewhere.
/// <para>
/// The predicate has two subtleties worth pinning: a network-path reference such as <c>//attacker.com</c> is a
/// perfectly <b>well-formed relative</b> reference that browsers resolve off-origin, and the app itself produces
/// base-relative return urls <b>without</b> a leading slash (See <c>NavigationManagerExtensions.GetRelativePath</c>,
/// which <c>AppShell</c> feeds straight into a <c>return-url</c>), so a rooted-only predicate would break the app's
/// own sign-in links.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ReturnUrlPredicateTests
{
    /// <summary>
    /// Values the app itself generates, plus the rooted form the e-mailed links carry. All must survive.
    /// </summary>
    [TestMethod]
    [DataRow("/")]
    [DataRow("/products/1")]
    [DataRow("/sign-in?otp=123456")]
    [DataRow("/confirm?email=a%40b.c&emailToken=123456")]
    // GetRelativePath() returns a BASE-relative path, i.e. no leading slash - this is what AppShell and
    // SignInModalService put into a return-url, so rejecting it would break ordinary sign-in.
    [DataRow("products/1")]
    [DataRow("sign-in")]
    [DataRow("products/1?a=b")]
    public void IsAppRelativeUrl_Should_AcceptTheUrlsTheAppItselfProduces(string url)
    {
        Assert.IsTrue(Uri.IsAppRelativeUrl(url, requireLeadingSlash: false),
            $"'{url}' is produced by the app itself and must not be rejected.");
    }

    /// <summary>
    /// Everything that can leave the origin. The <c>//</c> and <c>/\</c> forms are the interesting ones: both are
    /// well-formed RELATIVE references, so <c>Uri.IsWellFormedUriString(_, UriKind.Relative)</c> alone would let them
    /// through and the browser would then resolve them to the attacker's host.
    /// </summary>
    [TestMethod]
    [DataRow("//evil.example")]
    [DataRow("//evil.example/path")]
    [DataRow("///evil.example")]
    [DataRow("/\\evil.example")]
    [DataRow("\\\\evil.example")]
    [DataRow("\\/evil.example")]
    [DataRow("https://evil.example")]
    [DataRow("http://evil.example/x")]
    [DataRow("app://localhost/x")]
    [DataRow("javascript:alert(1)")]
    [DataRow("")]
    public void IsAppRelativeUrl_Should_RejectAnythingThatCanLeaveTheOrigin(string url)
    {
        Assert.IsFalse(Uri.IsAppRelativeUrl(url, requireLeadingSlash: false),
            $"'{url}' can escape this app's origin and must be rejected.");
        Assert.IsFalse(Uri.IsAppRelativeUrl(url),
            $"'{url}' must also be rejected by the stricter, rooted-only form.");
    }

    /// <summary>
    /// The default (rooted-only) form is what the Blazor Hybrid local HTTP server's callback uses, and it must stay
    /// strict: a rootless value is origin-bound but is not a valid app-rooted url.
    /// </summary>
    [TestMethod]
    public void IsAppRelativeUrl_Should_StillRequireALeadingSlashByDefault()
    {
        Assert.IsFalse(Uri.IsAppRelativeUrl("products/1"), "The default overload must keep requiring a leading slash.");
        Assert.IsTrue(Uri.IsAppRelativeUrl("/products/1"), "A rooted url must pass the default overload.");
    }
}

/// <summary>
/// The server half of the same boundary: <c>returnUrl</c> arrives in an <b>anonymous</b> request body and is spliced
/// into a link the application e-mails from its own domain, so an off-origin value would turn the app's own
/// transactional mail into a redirector. These tests read the captured e-mail (See <see cref="TestIdentityEmailService"/>)
/// and assert what actually left the building.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ReturnUrlHardeningTests
{
    /// <summary>
    /// <c>SendOtp</c> needs no pre-existing account - it auto-provisions - so this is reachable by an anonymous caller
    /// against any address they choose, which is what makes it worth a test rather than a comment.
    /// </summary>
    [TestMethod]
    public async Task SendOtp_Should_NotPutAnOffOriginReturnUrlIntoTheConfirmationEmail()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var email = MagicLinkSignInUtils.NewTestEmail();

        // A brand-new address: the account is created unconfirmed, so SendOtp sends the CONFIRMATION mail, and the
        // attacker-chosen returnUrl is what would be embedded in its link.
        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email, ReturnUrl = "https://evil.example/login" }, null, TestContext.CancellationToken),
            "A brand-new account is unconfirmed, so SendOtp must answer UserIsNotConfirmed after mailing the confirmation link.");

        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        var returnUrl = HttpUtility.ParseQueryString(captured.Link!.Query)["return-url"];

        Assert.AreEqual(PageUrls.Home, returnUrl,
            $"An off-origin return url must be replaced by the home page, but the e-mailed link carried '{returnUrl}'.");
    }

    /// <summary>
    /// The same boundary on the reset-password flow, which takes its return url from a different DTO and builds its
    /// link in a different method - so it can (and did) regress independently. A network-path reference is the
    /// interesting payload here: it is a well-formed RELATIVE url, so it slips past the obvious check.
    /// </summary>
    [TestMethod]
    [DataRow("//evil.example/login", PageUrls.Home, "A network-path reference is a well-formed RELATIVE url, so it must be caught explicitly.")]
    [DataRow("https://evil.example/login", PageUrls.Home, "An absolute url must never survive into an e-mailed link.")]
    // The guard must not become "replace everything": an ordinary in-app return url has to survive end to end, or
    // resetting a password from a product page would drop the user on the home page instead of bringing them back.
    [DataRow("/settings/sessions", "/settings/sessions", "A legitimate app-relative return url must be preserved.")]
    public async Task SendResetPasswordToken_Should_OnlyEmailAnAppRelativeReturnUrl(string requested, string expected, string because)
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        // A per-run account, not the seeded one: SendResetPasswordToken is throttled per user for the token's whole
        // lifetime, so sharing an account across these rows (they run in parallel) would fail on the resend delay.
        var email = await CreateConfirmedAccount(server, identityController);

        await identityController.SendResetPasswordToken(
            new() { Email = email, ReturnUrl = requested }, TestContext.CancellationToken);

        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.ResetPassword, TestContext.CancellationToken);

        var returnUrl = HttpUtility.ParseQueryString(captured.Link!.Query)["return-url"];

        Assert.AreEqual(expected, returnUrl, $"{because} The e-mailed link carried '{returnUrl}'.");
    }

    /// <summary>
    /// Registers and confirms a brand-new account, so the test owns its own fixture instead of mutating the seeded
    /// one. <c>SendOtp</c> auto-provisions the (unconfirmed) account and mails the confirmation code; consuming that
    /// code confirms it, which is what <c>SendResetPasswordToken</c> requires.
    /// </summary>
    private async Task<string> CreateConfirmedAccount(AppTestServer server, IIdentityController identityController)
    {
        var email = MagicLinkSignInUtils.NewTestEmail();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken),
            "A brand-new account is unconfirmed, so SendOtp answers UserIsNotConfirmed after mailing the confirmation link.");

        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        await identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, TestContext.CancellationToken);

        return email;
    }

    public TestContext TestContext { get; set; } = default!;
}
