using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class IdentityPagesTests : PageTestBase
{
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
        var signinPage = new SignInPage(Page, ServerAddress);

        await signinPage.Open();
        await signinPage.SignIn();
    }

    [TestMethod]
    public async Task SignIn_Should_Fail_With_InvalidCredentials()
    {
        var signinPage = new SignInPage(Page, ServerAddress);

        await signinPage.Open();
        await signinPage.SignIn(password: "invalid", isValidCredentials: false);
    }

    [TestMethod]
    public async Task SignOut_Should_WorkAsExpected()
    {
        var signinPage = new SignInPage(Page, ServerAddress);

        await signinPage.Open();
        await signinPage.SignIn();
        await signinPage.SignOut();
    }

    [TestMethod]
    public async Task SignUp_Should_Work_With_MagicLink()
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            var descriptor = ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>();
            services.Replace(descriptor);
        }).Start();

        var signupPage = new SignUpPage(Page, testServer.ServerAddress);

        await signupPage.Open();
        await signupPage.SignUp();
    }

    [TestMethod]
    public async Task SignUp_Should_Work_With_OtpCode()
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            var descriptor = ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>();
            services.Replace(descriptor);
        }).Start();

        var signupPage = new SignUpPage(Page, testServer.ServerAddress);

        await signupPage.Open();
        await signupPage.SignUp(usingMagicLink: false);
    }
}
