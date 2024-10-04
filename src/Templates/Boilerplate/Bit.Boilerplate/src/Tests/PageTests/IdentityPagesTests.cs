using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class IdentityPagesTests : PageTestBase
{
    private string signInText = "Sign in"; //TODO: multi-language test
    private string signOutText = "Sign out"; //TODO: multi-language test
    private string invalidCredentials = "Invalid user credentials"; //TODO: multi-language test

    [TestMethod]
    public async Task UnauthorizedUser_Should_RedirectToSignInPage()
    {
        var response = await Page.GotoAsync(new Uri(ServerAddress, Urls.ProfilePage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Expect(Page).ToHaveURLAsync(new Uri(ServerAddress, "/sign-in?return-url=profile").ToString());
    }

    [TestMethod]
    public async Task SignIn_Should_Work_With_ValidCredentials()
    {
        var singinPage = new SingInPage(Page, ServerAddress);

        await singinPage.GotoAsync();
        await singinPage.SignInAsync();

        await Expect(Page).ToHaveURLAsync(ServerAddress.ToString());
        await Expect(Page.Locator(".persona")).ToBeVisibleAsync();
        await Expect(Page.Locator(".persona")).ToContainTextAsync("Boilerplate test account");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = signOutText })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = signInText })).ToBeVisibleAsync(new() { Visible = false });
    }

    [TestMethod]
    public async Task SignIn_Should_Fail_With_InvalidCredentials()
    {
        var singinPage = new SingInPage(Page, ServerAddress);

        await singinPage.GotoAsync();
        await singinPage.SignInAsync(password: "invalid_password");

        await Expect(Page.GetByText(invalidCredentials)).ToBeVisibleAsync();
        await Expect(Page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = signInText })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = signOutText })).ToBeVisibleAsync(new() { Visible = false });
    }

    [TestMethod]
    public async Task SignOut_Should_WorkAsExpected()
    {
        var singinPage = new SingInPage(Page, ServerAddress);

        await singinPage.GotoAsync();
        await singinPage.SignInAsync();
        await singinPage.SignOutAsync();

        await Expect(Page).ToHaveURLAsync(ServerAddress.ToString());
        await Expect(Page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = signInText })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = signOutText })).ToBeVisibleAsync(new() { Visible = false });
    }

    [TestMethod]
    public async Task SignUp_Should_WorkAsExpected()
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            var descriptor = ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>();
            services.Replace(descriptor);
        }).StartAsync();

        var singupPage = new SingUpPage(Page, testServer.ServerAddress);

        await singupPage.GotoAsync();
        await singupPage.SingUpAsync();
    }
}