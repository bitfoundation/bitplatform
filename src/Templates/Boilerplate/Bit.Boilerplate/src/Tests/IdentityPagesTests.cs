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

        const string email = "test@bitplatform.dev";
        const string password = "123456";
        const string userFullName = "Boilerplate test account";

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = userFullName })).ToBeVisibleAsync();
        await Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(userFullName);
        await Expect(Page.Locator(".bit-prs.persona").Last).ToContainTextAsync(userFullName);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.RecordVideoDir = GetVideoDirectory(TestContext);
        return options;
    }

    [TestCleanup]
    public async ValueTask Cleanup()
    {
        await Context.CloseAsync();
        if (TestContext.CurrentTestOutcome is not UnitTestOutcome.Failed)
        {
            var directory = GetVideoDirectory(TestContext);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static string GetVideoDirectory(TestContext testContext)
    {
        var testMethodFullName = $"{testContext.FullyQualifiedTestClassName}.{testContext.TestName}";
        return Path.Combine(testContext.TestResultsDirectory!, "..", "..", "Videos", testMethodFullName);
    }
}
