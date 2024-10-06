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
    [DataRow("fa-IR")]
    [DataRow("en-US")]
    public async Task SignIn_Should_Work_With_ValidCredentials(string culture)
    {
        var signinPage = new SignInPage(Page, ServerAddress, culture);

        await signinPage.Goto();
        await signinPage.SignIn();
    }

    [TestMethod]
    [DataRow("fa-IR")]
    [DataRow("en-US")]
    public async Task SignIn_Should_Fail_With_InvalidCredentials(string culture)
    {
        var signinPage = new SignInPage(Page, ServerAddress, culture);

        await signinPage.Goto();
        await signinPage.SignIn(password: "invalid", isValidCredentials: false);
    }

    [TestMethod]
    [DataRow("fa-IR")]
    [DataRow("en-US")]
    public async Task SignOut_Should_WorkAsExpected(string culture)
    {
        var signinPage = new SignInPage(Page, ServerAddress, culture);

        await signinPage.Goto();
        await signinPage.SignIn();
        await signinPage.SignOut();
    }

    [TestMethod]
    [DataRow("fa-IR")]
    [DataRow("en-US")]
    public async Task SignUp_Should_Work_With_MagicLink(string culture)
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            var descriptor = ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>();
            services.Replace(descriptor);
        }).Start();

        var signupPage = new SignUpPage(Page, testServer.ServerAddress, culture);

        await signupPage.Goto();
        await signupPage.SignUp();
    }

    [TestMethod]
    [DataRow("fa-IR")]
    [DataRow("en-US")]
    public async Task SignUp_Should_Work_With_OtpCode(string culture)
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            var descriptor = ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>();
            services.Replace(descriptor);
        }).Start();

        var signupPage = new SignUpPage(Page, testServer.ServerAddress, culture);

        await signupPage.Goto();
        await signupPage.SignUp(usingMagicLink: false);
    }
}