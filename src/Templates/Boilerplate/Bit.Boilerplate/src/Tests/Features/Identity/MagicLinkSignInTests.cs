using System.Text.RegularExpressions;
using Boilerplate.Tests.Infrastructure.Components;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class MagicLinkSignInTests : AppPageTest
{
    /// <summary>
    /// A brand-new user signs in with her e-mail using the one-time-password: she asks for the code, reads it from the
    /// confirmation e-mail the test server captured, then types the 6 digits into the OTP panel to get signed in.
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingEmailedOtpCode()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        var email = MagicLinkSignInUtils.NewTestEmail();

        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(Page, serverAddress, email);

        var (_, otpCode) = await MagicLinkSignInUtils.ReadConfirmationEmail(
            server, email, TestContext.CancellationToken);

        Assert.MatchesRegex(new Regex(@"^\d{6}$"), otpCode,
            "The one-time-password read from the confirmation e-mail should be a 6 digit code.");

        await BitOtpInputUtils.FillOtpInputs(Page, otpCode);

        // Filling the last digit signs her in and lands on the home page.
        await Expect(Page).ToHaveURLAsync(serverAddress.ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    /// <summary>
    /// A brand-new user signs in by opening the magic link from her e-mail instead of typing the OTP: she asks for it,
    /// reads the link from the confirmation e-mail the test server captured, and opening it signs her in automatically.
    /// </summary>
    [TestMethod]
    public async Task User_Should_SignIn_UsingMagicLink()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        var email = MagicLinkSignInUtils.NewTestEmail();

        // Same starting point as the OTP test: she asks for a magic link / OTP for her brand-new e-mail.
        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(Page, serverAddress, email);

        var (confirmUrl, _) = await MagicLinkSignInUtils.ReadConfirmationEmail(
            server, email, TestContext.CancellationToken);

        // Instead of typing the code, she opens the magic link, which confirms her e-mail, signs her in and redirects home.
        await Page.GotoAsync(confirmUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Expect(Page).ToHaveURLAsync(serverAddress.ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }
}
