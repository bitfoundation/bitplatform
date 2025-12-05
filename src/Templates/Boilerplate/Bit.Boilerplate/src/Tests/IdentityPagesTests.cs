using Boilerplate.Tests.Extensions;

namespace Boilerplate.Tests;

[TestClass, TestCategory("UITest")]
public partial class IdentityPagesTests : PageTest
{
    [TestMethod]
    public async Task UnauthorizedUser_Should_RenderNotAuthorizedComponent()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Settings).ToString());

        await Expect(Page).ToHaveTitleAsync(AppStrings.NotAuthorizedPageTitle);
    }

    [TestMethod]
    public async Task SignIn_Should_WorkAsExpected()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await Page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.SignIn).ToString());

        await Expect(Page).ToHaveTitleAsync(AppStrings.SignInPageTitle);

        const string email = TestData.DefaultTestEmail;
        const string password = TestData.DefaultTestPassword;
        const string userFullName = TestData.DefaultTestFullName;

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

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
