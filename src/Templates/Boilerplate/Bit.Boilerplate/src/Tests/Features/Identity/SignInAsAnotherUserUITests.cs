namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class SignInAsAnotherUserUITests : AppPageTest
{
    [TestMethod]
    public async Task SigningInAsAnotherUser_Should_KeepBothAccounts_AndSwitchingBetweenThem_Should_UpdateTheOtherTabs()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var firstEmail = MagicLinkSignInUtils.NewTestEmail();
        var secondEmail = MagicLinkSignInUtils.NewTestEmail();

        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, firstEmail, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);

        var otherTab = await Context.NewPageAsync();
        await otherTab.GotoAsync(server.WebAppAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(otherTab.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);

        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, secondEmail, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(secondEmail);
        await Expect(otherTab.Locator(".bit-prs.persona").First).ToContainTextAsync(secondEmail);

        Assert.AreEqual(1, await CountUserSessions(server, firstEmail),
            "Signing in as somebody else must keep the first account on the device, signed in.");

        await AppMenuUtils.ClickItem(Page, callout => callout.Locator(".account-button", new() { HasText = firstEmail }));

        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);
        await Expect(otherTab.Locator(".bit-prs.persona").First).ToContainTextAsync(firstEmail);

        await AppMenuUtils.ClickItem(Page, AppStrings.SignOut);
        await Page.GetByText(AppStrings.SignOutOfAllAccounts).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).Last.ClickAsync();

        await Expect(Page.Locator("header .bit-drm-btn").First).Not.ToContainTextAsync(firstEmail);
        await Expect(otherTab.Locator("header .bit-drm-btn").First).Not.ToContainTextAsync(firstEmail);

        Assert.AreEqual(0, await CountUserSessions(server, firstEmail));
        Assert.AreEqual(0, await CountUserSessions(server, secondEmail), "Signing out of all accounts must end the session of the account that was not active too.");
    }

    [TestMethod]
    public async Task ConfirmingAnotherAccount_Should_KeepTheAccountThatWasSignedIn()
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

        Assert.AreEqual(1, await CountUserSessions(server, firstEmail),
            "Confirming another account signs it in next to the one that was signed in, which must stay signed in.");
    }

    [TestMethod]
    public async Task AFailedSignIn_Should_KeepTheSessionTheUserAlreadyHas()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var email = MagicLinkSignInUtils.NewTestEmail();

        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);

        await Page.GotoAsync(new Uri(server.WebAppAddress, PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        await SignInPanelUtils.FillCredentials(Page, email, "not-the-password");
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(AppStrings.InvalidUserCredentials).First).ToBeVisibleAsync();

        await Page.GotoAsync(server.WebAppAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(email);

        Assert.AreEqual(1, await CountUserSessions(server, email),
            "A sign-in the server refused must not end the session the user already had.");
    }

    private async Task<int> CountUserSessions(AppTestServer server, string email)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var normalizedEmail = email.ToUpperInvariant();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserSessions
            .CountAsync(us => us.User!.NormalizedEmail == normalizedEmail, TestContext.CancellationToken);
    }
}
