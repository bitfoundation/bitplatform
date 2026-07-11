using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class MagicLinkSignInTests : PageTest
{
    /// <summary>
    /// A brand-new user signs in with her e-mail using the one-time-password: she asks for the code, reads it from the
    /// confirmation e-mail that the dev environment logs, then types the 6 digits into the OTP panel to get signed in.
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingEmailedOtpCode()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        var email = MagicLinkSignInUtils.NewTestEmail();

        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(Page, server.WebAppServerAddress, email);

        var (_, otpCode) = await MagicLinkSignInUtils.ReadConfirmationEmailFromDiagnosticLog(
            server, email, TestContext.CancellationToken);

        Assert.MatchesRegex(new Regex(@"^\d{6}$"), otpCode,
            "The one-time-password read from the confirmation e-mail should be a 6 digit code.");

        await MagicLinkSignInUtils.FillOtpInputs(Page, otpCode);

        // Filling the last digit signs her in and lands on the home page.
        await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    /// <summary>
    /// A brand-new user signs in by opening the magic link from her e-mail instead of typing the OTP: she asks for it,
    /// reads the link from the confirmation e-mail that the dev environment logs, and opening it signs her in automatically.
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingMagicLink()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        var email = MagicLinkSignInUtils.NewTestEmail();

        // Same starting point as the OTP test: she asks for a magic link / OTP for her brand-new e-mail.
        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(Page, server.WebAppServerAddress, email);

        var (confirmUrl, _) = await MagicLinkSignInUtils.ReadConfirmationEmailFromDiagnosticLog(
            server, email, TestContext.CancellationToken);

        // Instead of typing the code, she opens the magic link, which confirms her e-mail, signs her in and redirects home.
        await Page.GotoAsync(confirmUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    public override BrowserNewContextOptions ContextOptions() => base.ContextOptions().EnableVideoRecording(TestContext);

    [TestCleanup]
    public async ValueTask Cleanup() => await Context.FinalizeVideoRecording(TestContext);
}
