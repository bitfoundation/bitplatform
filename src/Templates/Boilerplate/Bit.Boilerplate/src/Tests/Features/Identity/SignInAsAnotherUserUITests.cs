namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class SignInAsAnotherUserUITests : AppPageTest
{
    [TestMethod]
    public async Task SigningInAsAnotherUser_Should_EndTheFirstSession_AndUpdateTheOtherTabs()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var firstEmail = MagicLinkSignInUtils.NewTestEmail();
        var secondEmail = MagicLinkSignInUtils.NewTestEmail();

        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, firstEmail, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);

        Assert.AreEqual(1, await CountUserSessions(server, firstEmail));

        var otherTab = await Context.NewPageAsync();
        await otherTab.GotoAsync(server.WebAppAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(otherTab.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);

        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, secondEmail, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(secondEmail);

        Assert.AreEqual(0, await CountUserSessions(server, firstEmail),
            "Signing in as somebody else must end the session it replaces instead of leaving it behind in the sessions list.");

        await Expect(otherTab.Locator(".bit-prs.persona").First).ToContainTextAsync(secondEmail);
    }

    [TestMethod]
    public async Task ConfirmingAnotherAccount_Should_EndTheSessionItReplaces()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var firstEmail = MagicLinkSignInUtils.NewTestEmail();
        var secondEmail = MagicLinkSignInUtils.NewTestEmail();

        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, firstEmail, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);

        await MagicLinkSignInUtils.RequestMagicLinkAndOtp(Page, server.WebAppAddress, secondEmail);

        var confirmation = await server.WaitForCapturedEmail(secondEmail, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppAddress, confirmation.Link!.PathAndQuery).ToString());
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(secondEmail);

        Assert.AreEqual(0, await CountUserSessions(server, firstEmail),
            "Confirming another account signs it in, so it must end the session it replaces instead of leaving it behind.");
    }

    private async Task<int> CountUserSessions(AppTestServer server, string email)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var normalizedEmail = email.ToUpperInvariant();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserSessions
            .CountAsync(us => us.User!.NormalizedEmail == normalizedEmail, TestContext.CancellationToken);
    }
}
