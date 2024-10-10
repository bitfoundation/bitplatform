//+:cnd:noEmit
using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Services;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class IdentityPagesTests : PageTestBase
{
    [TestMethod]
    public async Task UnauthorizedUser_Should_RedirectToSignInPage()
    {
        var response = await Page.GotoAsync(new Uri(WebAppServerAddress, Urls.ProfilePage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Expect(Page).ToHaveURLAsync(new Uri(WebAppServerAddress, "/sign-in?return-url=profile").ToString());
    }

    [TestMethod]
    public async Task SignIn_Should_Work_With_ValidCredentials()
    {
        var signinPage = new SignInPage(Page, WebAppServerAddress);

        await signinPage.Open();
        await signinPage.AssertOpen();

        var signedInPage = await signinPage.SignIn();
        await signedInPage.AssertSignInSuccess();
    }

    [TestMethod]
    public async Task SignIn_Should_Fail_With_InvalidCredentials()
    {
        var signinPage = new SignInPage(Page, WebAppServerAddress);

        await signinPage.Open();
        await signinPage.AssertOpen();

        await signinPage.SignIn(email: "invalid@bitplatform.dev", password: "invalid");
        await signinPage.AssertSignInFailed();
    }

    [TestMethod]
    [Authenticated]
    public async Task SignOut_Should_WorkAsExpected()
    {
        var homePage = new IdentityLayout(Page, WebAppServerAddress, Urls.HomePage, AppStrings.HomeTitle);

        await homePage.Open();
        await homePage.AssertOpen();

        await homePage.SignOut();
        await homePage.AssertSignOut();
    }

    [TestMethod]
    public async Task SignUp_Should_Work_With_MagicLink()
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            //#if (captcha == "reCaptcha")
            services.Replace(ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>());
            //#endif
        }).Start();

        var signupPage = new SignUpPage(Page, testServer.WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        await signupPage.SignUp();
        await signupPage.AssertSignUp();

        await signupPage.OpenEmail();
        await signupPage.AssertEmailContent();

        await signupPage.ConfirmByMagicLink();
        await signupPage.AssertConfirm();
    }

    [TestMethod]
    public async Task SignUp_Should_Work_With_OtpCode()
    {
        await using var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            //#if (captcha == "reCaptcha")
            services.Replace(ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>());
            //#endif
        }).Start();

        var signupPage = new SignUpPage(Page, testServer.WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        await signupPage.SignUp();
        await signupPage.AssertSignUp();

        await signupPage.OpenEmail();
        await signupPage.AssertEmailContent();

        await signupPage.ConfirmByOtp();
        await signupPage.AssertConfirm();
    }
}
