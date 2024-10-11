//+:cnd:noEmit
using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Services;
using Boilerplate.Tests.PageTests.PageModels.Layout;
using Boilerplate.Tests.PageTests.PageModels.Identity;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class IdentityPagesTests : PageTestBase
{
    [TestMethod]
    public async Task UnauthorizedUser_Should_RedirectToSignInPage()
    {
        var response = await Page.GotoAsync(new Uri(WebAppServerAddress, Urls.SettingsPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Expect(Page).ToHaveURLAsync(new Uri(WebAppServerAddress, "/sign-in?return-url=settings").ToString());
    }

    [TestMethod]
    public async Task SignIn_Should_Work_With_ValidCredentials()
    {
        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var signedInPage = await signInPage.SignIn();
        await signedInPage.AssertSignInSuccess();
    }

    [TestMethod]
    public async Task SignIn_Should_Fail_With_InvalidCredentials()
    {
        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        await signInPage.SignIn(email: "invalid@bitplatform.dev", password: "invalid");
        await signInPage.AssetNotSignedIn();
    }

    [TestMethod]
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
        await TestServer.Build(services =>
        {
            //#if (captcha == "reCaptcha")
            services.Replace(ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>());
            //#endif
        }).Start();

        var signupPage = new SignUpPage(Page, WebAppServerAddress);

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
        await TestServer.Build(services =>
        {
            //#if (captcha == "reCaptcha")
            services.Replace(ServiceDescriptor.Transient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>());
            //#endif
        }).Start();

        var signupPage = new SignUpPage(Page, WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        await signupPage.SignUp();
        await signupPage.AssertSignUp();

        await signupPage.OpenEmail();
        await signupPage.AssertEmailContent();

        await signupPage.ConfirmByOtp();
        await signupPage.AssertConfirm();
    }

    protected override bool AutoStartTestServer(string method) => new string[]
    {
        nameof(SignUp_Should_Work_With_MagicLink),
        nameof(SignUp_Should_Work_With_OtpCode)
    }.Contains(method) is false;
}
