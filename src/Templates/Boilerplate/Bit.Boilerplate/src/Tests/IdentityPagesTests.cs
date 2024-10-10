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

        var response = await Page.GotoAsync(new Uri(server.WebAppServerAddress, Urls.ProfilePage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Expect(Page).ToHaveURLAsync(new Uri(server.WebAppServerAddress, "/sign-in?return-url=profile").ToString());
    }

    [TestMethod]
    public async Task SignIn_Should_WorkAsExpected()
    {
        await using var server = new AppTestServer();
        await server.Build().Start();

        var response = await Page.GotoAsync(new Uri(server.WebAppServerAddress, Urls.SignInPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);
        await Expect(Page).ToHaveTitleAsync(AppStrings.SignInTitle);

        const string email = "test@bitplatform.dev";
        const string password = "123456";
        const string userFullName = "Boilerplate test account";

        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(server.WebAppServerAddress.ToString());
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = userFullName })).ToBeVisibleAsync();
        await Expect(Page.GetByText(userFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(userFullName).Nth(1)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }
}
