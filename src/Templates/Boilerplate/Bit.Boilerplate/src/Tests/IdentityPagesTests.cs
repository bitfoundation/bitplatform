using Boilerplate.Tests.Extensions;

namespace Boilerplate.Tests;

[TestClass]
public partial class IdentityPagesTests : PageTest
{
    [TestMethod]
    public async Task UnauthorizedUser_Should_RedirectToSignInPage()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, Urls.SettingsPage).ToString());

        await Expect(Page).ToHaveURLAsync(new Uri(server.WebAppServerAddress, "/sign-in?return-url=settings").ToString());
    }

    [TestMethod]
    public async Task SignIn_Should_WorkAsExpected()
    {
        await using var server = new AppTestServer();
        await server.Build().Start();

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, Urls.SignInPage).ToString());

        await Expect(Page).ToHaveTitleAsync(AppStrings.SignInPageTitle);

        const string email = TestData.DefaultTestEmail;
        const string password = TestData.DefaultTestPassword;
        const string userFullName = TestData.DefaultTestFullName;

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn, Exact = true }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = userFullName })).ToBeVisibleAsync();
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(userFullName);
        await Expect(Page.Locator(".bit-prs.persona").Last).ToContainTextAsync(userFullName);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    public override BrowserNewContextOptions ContextOptions() => base.ContextOptions().EnableVideoRecording(TestContext);

    [TestCleanup]
    public async ValueTask Cleanup() => await Context.FinalizeVideoRecording(TestContext);
}
